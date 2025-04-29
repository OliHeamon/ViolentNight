using System;
using System.Collections.Generic;

namespace ViolentNight.Systems.AI;

public sealed class AIContainer(string initialState)
{
    public AIState CurrentState => aiStatesById[currentState];

    private string currentState = initialState;

    private readonly Dictionary<string, AIState> aiStatesById = [];

    public AIContainer AddState(AIState state)
    {
        aiStatesById[state.Identifier] = state;

        return this;
    }

    public void UpdateCurrentState()
    {
        if (aiStatesById.Count == 0)
        {
            return;
        }

        if (currentState == null)
        {
            throw new NullReferenceException("AI Container did not have a default state specified!");
        }

        CurrentState.StateRoutine.Invoke();
    }

    public void TransitionTo(string identifier)
    {
        CurrentState?.OnDeactivated?.Invoke();

        currentState = identifier;

        CurrentState?.OnActivated?.Invoke();
    }
}

public class AIState(string identifier, Action stateRoutine, Action onActivated = null, Action onDeactivated = null)
{
    public string Identifier { get; private set; } = identifier;

    public Action StateRoutine { get; private set; } = stateRoutine;

    public Action OnActivated { get; private set; } = onActivated;

    public Action OnDeactivated { get; private set; } = onDeactivated;
}
