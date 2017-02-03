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
			Delete
		}

		public enum State
		{
			NotAccepted,
			Accepted,
				WaitingForAllocation,
				Allocated,
			Deleted
		}

		private readonly StateMachine<State, Action> state = new StateMachine<State, Action>(State.NotAccepted);

		public SellSideOrder(int brokerId)
		{
			BrokerId = brokerId;
			state.Configure(State.NotAccepted)
				.Permit(Action.Accept, State.WaitingForAllocation);

			state.Configure(State.Accepted)
				.Permit(Action.Delete, State.Deleted);

			state.Configure(State.WaitingForAllocation)
				.SubstateOf(State.Accepted)
				.Permit(Action.Allocate, State.Allocated);

			state.Configure(State.Allocated)
				.SubstateOf(State.Accepted);

			state.OnTransitioned(StateChanged);
		}

		private void StateChanged(StateMachine<State, Action>.Transition t)
		{
			OnStateChange?.Invoke(this, EventArgs.Empty);
			//Console.WriteLine($"{t.Source} => {t.Destination}, [{string.Join(", ", state.PermittedTriggers)}]");
		}

		public int BrokerId { get; }
		public bool OrderAccepted => state.IsInState(State.Accepted);
		public bool IsAllocated => state.IsInState(State.Allocated);
		public bool IsDeleted => state.IsInState(State.Deleted);

		public State GetState() => state.State;

		public void Accept()
		{
			state.Fire(Action.Accept);
		}

		public void Allocate()
		{
			state.Fire(Action.Allocate);
		}

		public void Delete()
		{
			state.Fire(Action.Delete);
		}

		public event EventHandler OnStateChange;
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