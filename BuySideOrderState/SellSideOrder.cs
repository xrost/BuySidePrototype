using System;
using Stateless;

namespace BuySideOrderState
{
	public class SellSideOrder
	{
		public enum Action
		{
			Accept,
			Allocate,
			Delete,
			Reject,
			RejectCancel
		}

		public enum State
		{
			NotAccepted,
			Accepted,
				PendingAllocation,
				Allocated,
			Rejected,
			Deleted,
		}

		private readonly StateMachine<State, Action> state = new StateMachine<State, Action>(State.NotAccepted);

		public SellSideOrder(int brokerId)
		{
			BrokerId = brokerId;
			state.Configure(State.NotAccepted)
				.Permit(Action.Accept, State.PendingAllocation)
				.Permit(Action.Reject, State.Rejected);

			state.Configure(State.PendingAllocation)
				.SubstateOf(State.Accepted)
				.Permit(Action.Delete, State.Deleted)
				.Ignore(Action.RejectCancel)
				.Permit(Action.Allocate, State.Allocated);

			state.Configure(State.Allocated)
				.SubstateOf(State.Accepted)
				.Permit(Action.Delete, State.Deleted)
				.Ignore(Action.RejectCancel);

			state.OnTransitioned(StateChanged);
		}

		private void StateChanged(StateMachine<State, Action>.Transition t)
		{
			//Console.WriteLine($"{t.Source} => {t.Destination},  Actions: [{string.Join(" | ", state.PermittedTriggers)}]");
			OnStateChange?.Invoke(this, EventArgs.Empty);
		}

		public int BrokerId { get; }
		public bool IsAccepted => state.IsInState(State.Accepted);
		public bool IsAllocated => state.IsInState(State.Allocated);
		public bool IsDeleted => state.IsInState(State.Deleted);
		public bool IsRejected => state.IsInState(State.Rejected);
		public bool IsCancelRejected { get; private set; }

		public State GetState() => state.State;

		public void Accept() => state.Fire(Action.Accept);

		public void Reject() => state.Fire(Action.Reject);

		public void Allocate() => state.Fire(Action.Allocate);

		public void Delete() => state.Fire(Action.Delete);

		public void RejectCancel()
		{
			state.Fire(Action.RejectCancel);
			IsCancelRejected = true;
			OnStateChange?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler OnStateChange;

		public static void Test()
		{
			var order = new SellSideOrder(1);
			order.Accept();
			order.RejectCancel();
			//order.Allocate();
			//order.Delete();
		}
	}

	public static class BrokerTest
	{
		public static void Test()
		{
			var broker = new SellSideOrder(100);
			broker.Accept();
			broker.Allocate();
			broker.Delete();
		}
	}
}