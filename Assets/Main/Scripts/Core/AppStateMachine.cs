using System;

namespace Main.Core
{
    public enum AppState
    {
        Boot = 0,
        Auth = 1,
        Map = 2,
        AR = 3
    }

    public sealed class AppStateMachine
    {
        public AppState CurrentState { get; private set; } = AppState.Boot;

        public event Action<AppState, AppState> OnStateChanged;

        public void SetState(AppState nextState)
        {
            if (nextState == CurrentState)
            {
                return;
            }

            var previous = CurrentState;
            CurrentState = nextState;
            OnStateChanged?.Invoke(previous, nextState);
        }
    }
}
