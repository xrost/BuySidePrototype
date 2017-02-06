using System;
using System.Collections.Generic;
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
			DoesNotExist,
			Accepted,
				PendingAllocation,
				Allocated,
			NotAccepted,
				Rejected,
				Deleted
		}

		private readonly StateMachine<State, Action> state = new StateMachine<State, Action>(State.DoesNotExist);

		public SellSideOrder(int brokerId)
		{
			BrokerId = brokerId;
			state.Configure(State.DoesNotExist)
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
				.Ignore(Action.Allocate)
				.Ignore(Action.RejectCancel);

			state.Configure(State.Rejected)
				.SubstateOf(State.NotAccepted);

			state.Configure(State.Deleted)
				.SubstateOf(State.NotAccepted);

			state.OnTransitioned(StateChanged);
		}

		private void StateChanged(StateMachine<State, Action>.Transition t)
		{
			//Console.WriteLine($"{t.Source} => {t.Destination},  Actions: [{string.Join(" | ", state.PermittedTriggers)}]");
			OnStateChange?.Invoke(this, EventArgs.Empty);
		}

		public int BrokerId { get; }
		public bool HasResponse => !state.IsInState(State.DoesNotExist);
		public bool IsAccepted => state.IsInState(State.Accepted);
		public bool NotAccepted => state.IsInState(State.NotAccepted);
		public bool IsAllocated => state.IsInState(State.Allocated);
		public bool IsDeleted => state.IsInState(State.Deleted);
		public bool IsRejected => state.IsInState(State.Rejected);
		public bool IsCancelRejected { get; private set; }

		public State GetState() => state.State;

		public void Accept() => state.Fire(Action.Accept);

		public void Reject() => state.Fire(Action.Reject);

		public void Allocate() => state.Fire(Action.Allocate);

		public void Delete()
		{
			if (state.CanFire(Action.Delete))
				IsCancelRejected = false;
			state.Fire(Action.Delete);
		}

		public void RejectCancel()
		{
			state.Fire(Action.RejectCancel);
			IsCancelRejected = true;
			OnStateChange?.Invoke(this, EventArgs.Empty);
		}

		public IEnumerable<Action> AllowedActions => state.PermittedTriggers;

		public event EventHandler OnStateChange;
	}
}