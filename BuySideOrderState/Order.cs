using System;
using System.Collections.Generic;
using System.Linq;
using Stateless;

namespace BuySideOrderState
{
	public class Order
	{
		private readonly StateMachine<State, Trigger> state = new StateMachine<State, Trigger>(State.BuySide);
		private readonly BuySide buySide = new BuySide();
		private readonly SellSide sellSide = new SellSide(3);
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderAcceptedTrigger;
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderDeletedTrigger;
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderAllocatedTrigger;
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderRejectedTrigger;
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> cancelRejectedTrigger;

		public Order()
		{
			SellSide = new SellSideWrapper(sellSide, this);
			SubscribeToSellSideEvents();

			orderAcceptedTrigger = state.SetTriggerParameters<int>(Trigger.OrderAccepted);
			orderDeletedTrigger = state.SetTriggerParameters<int>(Trigger.OrderDeleted);
			orderAllocatedTrigger = state.SetTriggerParameters<int>(Trigger.OrderAllocated);
			orderRejectedTrigger = state.SetTriggerParameters<int>(Trigger.OrderRejected);
			cancelRejectedTrigger = state.SetTriggerParameters<int>(Trigger.CancelRejected);

			state.Configure(State.BuySide)
				.Ignore(Trigger.AddBuySideOrder)
				.Permit(Trigger.BuySideCompleted, State.SellSide)
				.Permit(Trigger.CloseOrder, State.Closed);

			state.Configure(State.SellSide)
				.OnEntryFrom(Trigger.BuySideCompleted, () => OnSellSide?.Invoke(this, EventArgs.Empty))
				.InternalTransition(orderAcceptedTrigger, OnOrderAccepted)
				.InternalTransition(orderRejectedTrigger, OnOrderRejected)
				.InternalTransition(orderAllocatedTrigger, (brokerId, t) => sellSide.AllocateOrder(brokerId))
				.InternalTransition(orderDeletedTrigger, OrderDeleted)
				.Permit(Trigger.CloseOrder, State.Closed)
				.Permit(Trigger.CancelBuySide, State.Cancelled);

			state.Configure(State.Cancelled)
				.OnEntry(NotifySellSideAboutCancellation)
				.InternalTransition(orderAcceptedTrigger, OnOrderAccepted)
				.InternalTransition(orderDeletedTrigger, OrderDeleted)
				.InternalTransition(orderRejectedTrigger, OnOrderRejected)
				.InternalTransition(orderAllocatedTrigger, (brokerId, t) => sellSide.AllocateOrder(brokerId))
				.InternalTransition(cancelRejectedTrigger, CancelRejected)
				.Permit(Trigger.UndoCancel, State.SellSide)
				.Permit(Trigger.CloseOrder, State.Closed);

			state.Configure(State.Closed)
				.OnEntryFrom(Trigger.CancelBuySide, (tr) => OnCancelConfirmed.Raise());

			state.OnTransitioned(RaiseStateChanged);

			sellSide.OnCancelRejected += (_, args) => state.Fire(Trigger.UndoCancel);

			var xml = state.ToDotGraph();
		}

		private void SubscribeToSellSideEvents()
		{
			SellSide.OnRejected += (_, args) => state.Fire(Trigger.CloseOrder);
			sellSide.OnCancelled += (_, args) => state.Fire(Trigger.CloseOrder);
			sellSide.OnCancelConfirmed += (_, args) => state.Fire(Trigger.CloseOrder);
		}

		private void RaiseStateChanged(StateMachine<State, Trigger>.Transition t)
		{
			//Console.WriteLine($"From: {t.Source} to {t.Destination}");
			OnStateChange?.Invoke(this, state.State);
		}

		private void OnOrderAccepted(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			sellSide.AcceptOrder(brokerId);
		}

		private void OnOrderRejected(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			sellSide.RejectOrder(brokerId);
		}

		private void CancelRejected(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			sellSide.RejectCancel(brokerId);
		}

		private void OrderDeleted(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			sellSide.DeleteOrder(brokerId);
		}

		private void NotifySellSideAboutCancellation()
		{
			sellSide.Cancel();
			OnBuySideCancel.Raise();
		}

		public int BuySideOrderCount => buySide.Count;

		public void AddBuySideOrder(object order)
		{
			state.Fire(Trigger.AddBuySideOrder);
			buySide.Add(order);
			if (buySide.IsCompleted())
				state.Fire(Trigger.BuySideCompleted);
		}

		public void CancelBuySide()
		{
			state.Fire(Trigger.CancelBuySide);
		}

		public void OrderCreated(int brokerId)
		{
			state.Fire(orderAcceptedTrigger, brokerId);
		}

		public void OrderAllocated(int brokerId)
		{
			state.Fire(orderAllocatedTrigger, brokerId);
		}

		public void OrderDeleted(int brokerId)
		{
			state.Fire(orderDeletedTrigger, brokerId);
		}

		public void OrderRejected(int brokerId)
		{
			state.Fire(orderRejectedTrigger, brokerId);
		}

		public void CancelRejected(int brokerId)
		{
			state.Fire(cancelRejectedTrigger, brokerId);
		}

		public State GetState() => state.State;

		public enum State
		{
			BuySide,
			SellSide,
			Cancelled,
			Closed
		}

		public enum Trigger
		{
			AddBuySideOrder,
			BuySideCompleted,
			CancelBuySide,
			OrderAccepted,
			OrderAllocated,
			OrderDeleted,
			OrderRejected,
			CancelRejected,
			UndoCancel,
			CloseOrder
		}

		public bool IsActionAvailable(Trigger action) => state.PermittedTriggers.Contains(action);

		public SellSideWrapper SellSide { get; }

		public class SellSideWrapper
		{
			private readonly SellSide sellSide;
			private readonly Order order;

			public SellSideWrapper(SellSide sellSide, Order order)
			{
				this.order = order;
				this.sellSide = sellSide;
				this.sellSide.OnFirstOrderAccepted += FirstOrderAccepted;
				this.sellSide.OnCancelled += (_, args) => order.OnCancelled.Raise();
				this.sellSide.OnCancelConfirmed += (_, args) => order.OnCancelConfirmed.Raise();
			}

			private void FirstOrderAccepted(object sender, EventArgs e)
			{
				if (order.state.State == State.SellSide)
					OnFirstOrderAccepted.Raise();
			}


			public IEnumerable<SellSideOrder> Orders => sellSide;

			public event EventHandler OnFirstOrderAccepted;

			public event EventHandler OnAllocated
			{
				add { sellSide.OnAllocated += value; }
				remove { sellSide.OnAllocated -= value; }
			}

			public event EventHandler OnCancelRejected
			{
				add { sellSide.OnCancelRejected += value; }
				remove { sellSide.OnCancelRejected -= value; }
			}

			public event EventHandler OnRejected
			{
				add { sellSide.OnRejected += value; }
				remove { sellSide.OnRejected -= value; }
			}
		}


		public event EventHandler<State> OnStateChange;

		public event EventHandler OnSellSide;

		public event EventHandler OnBuySideCancel;

		public event EventHandler OnCancelled;

		public event EventHandler OnCancelConfirmed;
	}
}
