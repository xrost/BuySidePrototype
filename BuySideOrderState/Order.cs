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
			SellSide = new SellSideWrapper(sellSide);

			orderAcceptedTrigger = state.SetTriggerParameters<int>(Trigger.OrderAccepted);
			orderDeletedTrigger = state.SetTriggerParameters<int>(Trigger.OrderDeleted);
			orderAllocatedTrigger = state.SetTriggerParameters<int>(Trigger.OrderAllocated);
			orderRejectedTrigger = state.SetTriggerParameters<int>(Trigger.OrderRejected);
			cancelRejectedTrigger = state.SetTriggerParameters<int>(Trigger.CancelRejected);

			state.Configure(State.BuySide)
				.Ignore(Trigger.AddBuySideOrder)
				.Permit(Trigger.BuySideCompleted, State.SellSide)
				.Permit(Trigger.CancelBuySide, State.Closed)
				.Permit(Trigger.CloseOrder, State.Closed);

			state.Configure(State.SellSide)
				.OnEntry(() => OnSellSide?.Invoke(this, EventArgs.Empty))
				.InternalTransition(orderAcceptedTrigger, OnOrderAccepted)
				.InternalTransition(orderRejectedTrigger, OnOrderRejected)
				.InternalTransition(orderAllocatedTrigger, (brokerId, t) => sellSide.AllocateOrder(brokerId))
				.InternalTransition(orderDeletedTrigger, OrderDeleted)
				.Permit(Trigger.CloseOrder, State.Closed)
				.Permit(Trigger.CancelBuySide, State.Cancelled)
				.OnExit(t =>
				{
					if (t.Destination == State.Cancelled)
						NotifySellSideAboutCancellation();
				});

			state.Configure(State.Cancelled)
				.InternalTransition(orderAcceptedTrigger, OnOrderAccepted)
				.InternalTransition(orderDeletedTrigger, OrderDeleted )
				.InternalTransition(cancelRejectedTrigger, CancelRejected)
				.Permit(Trigger.CloseOrder, State.Closed);

			state.OnTransitioned(RaiseStateChanged);

			var xml = state.ToDotGraph();
		}

		private void RaiseStateChanged(StateMachine<State, Trigger>.Transition t)
		{
			//Console.WriteLine($"From: {t.Source} to {t.Destination}");
			OnStateChange?.Invoke(this, state.State);
		}

		private void OnOrderAccepted(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			var firstAccepted = sellSide.AcceptOrder(brokerId);
			if (firstAccepted && t.Destination == State.SellSide)
				OnFirstOrderAccepted?.Invoke(this, EventArgs.Empty);
		}

		private void OnOrderRejected(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			if (sellSide.RejectOrder(brokerId))
			{
				OnRejected?.Invoke(this, EventArgs.Empty);
				state.Fire(Trigger.CloseOrder);
			}
		}

		private void CancelRejected(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			if (sellSide.RejectCancel(brokerId))
			{
				OnCancelRejected?.Invoke(this, EventArgs.Empty);
				state.Fire(Trigger.CloseOrder);
			}
		}

		private void OrderDeleted(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			sellSide.DeleteOrder(brokerId);
			if (sellSide.AllOrdersDeleted())
				state.Fire(Trigger.CloseOrder);
		}

		private void NotifySellSideAboutCancellation()
		{
			OnBuySideCancel?.Invoke(this, EventArgs.Empty);
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
			CloseOrder
		}

		public bool IsActionAvailable(Trigger action) => state.PermittedTriggers.Contains(action);

		public SellSideWrapper SellSide { get; }

		public class SellSideWrapper
		{
			private readonly SellSide sellSide;

			public SellSideWrapper(SellSide sellSide)
			{
				this.sellSide = sellSide;
			}

			public bool OrderExists(int brokerId) => sellSide.OrderExists(brokerId);
			public bool CanAllocate(int brokerId) => sellSide.CanAllocate(brokerId);
			public IEnumerable<SellSideOrder> Orders => sellSide;
		}


		public event EventHandler<State> OnStateChange;

		public event EventHandler OnSellSide;

		public event EventHandler OnBuySideCancel;

		public event EventHandler OnFirstOrderAccepted;

		public event EventHandler OnRejected;

		public event EventHandler OnCancelRejected;
	}
}