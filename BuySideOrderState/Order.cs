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
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderCreatedTrigger;
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderDeletedTrigger;
		private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> orderAllocatedTrigger;

		public Order()
		{
			SellSide = new SellSideWrapper(sellSide);

			orderCreatedTrigger = state.SetTriggerParameters<int>(Trigger.OrderCreated);
			orderDeletedTrigger = state.SetTriggerParameters<int>(Trigger.OrderDeleted);
			orderAllocatedTrigger = state.SetTriggerParameters<int>(Trigger.OrderAllocated);

			state.Configure(State.BuySide)
				.Ignore(Trigger.AddBuySideOrder)
				.Permit(Trigger.BuySideCompleted, State.SellSide)
				.Permit(Trigger.CancelBuySide, State.Cancelled)
				.Permit(Trigger.CloseOrder, State.Closed);

			state.Configure(State.SellSide)
				.OnEntry(() => OnSellSide?.Invoke(this, EventArgs.Empty))
				.InternalTransition(orderCreatedTrigger, OrderAccepted)
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
				.InternalTransition(orderCreatedTrigger, OrderAccepted)
				.InternalTransition(orderDeletedTrigger, OrderDeleted )
				.Permit(Trigger.CloseOrder, State.Closed);
				//.PermitIf(Trigger.CloseOrder, State.Closed, () => !sellSide.HasAcceptedOrders());

			state.OnTransitioned(RaiseStateChanged);

			var xml = state.ToDotGraph();
		}

		private void OrderAccepted(int brokerId, StateMachine<State, Trigger>.Transition t)
		{
			var wasEmpty = !sellSide.HasAcceptedOrders();
			sellSide.AcceptOrder(brokerId);
			if (wasEmpty && t.Destination == State.SellSide)
				OnFirstOrderAccepted?.Invoke(this, EventArgs.Empty);
		}

		private void RaiseStateChanged(StateMachine<State, Trigger>.Transition t)
		{
			Console.WriteLine($"From: {t.Source} to {t.Destination}");
			OnStateChange?.Invoke(this, state.State);
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
			state.Fire(orderCreatedTrigger, brokerId);
		}

		public void OrderAllocated(int brokerId)
		{
			state.Fire(orderAllocatedTrigger, brokerId);
		}

		public void OrderDeleted(int brokerId)
		{
			state.Fire(orderDeletedTrigger, brokerId);
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
			OrderCreated,
			OrderAllocated,
			OrderDeleted,
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
	}
}