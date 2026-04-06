class State<TState>
{
	public Action? OnEnter { get; set; }
	public Action? OnUpdate { get; set; }
	public Action? OnExit { get; set; }

	public State()
	{

	}

	public State(Action? onEnter = null, Action? onUpdate = null, Action? onExit = null)
	{
		OnEnter = onEnter;
		OnUpdate = onUpdate;
		OnExit = onExit;
	}
}

class Transition<TState>
{
	public TState To { get; }
	public string? TriggerName { get; }
	public Func<bool>? Condition { get; }

	public Transition(TState to, string? trigger = null, Func<bool>? condition = null)
	{
		To = to;
		TriggerName = trigger;
		Condition = condition;
	}

	public bool CanTransition()
	{
		return Condition == null || Condition();
	}
}

class StateMachine<TState> where TState : Enum
{
	public Action? OnAnyEnter { get; set; } // Invoked after specific state OnEnter
	public Action? OnAnyExit { get; set; } // Invoked after specific state OnExit

	private readonly Dictionary<TState, State<TState>> _states = new();
	private readonly List<Transition<TState>> _anyTransitions = new(); // any state transitions
	private readonly Dictionary<TState, List<Transition<TState>>> _transitions = new();
	private readonly HashSet<string> _triggers = new();

	private TState? _currentStateKey;
	private State<TState>? _currentState;

	public State<TState> AddState(TState key, State<TState> state)
	{
		_states[key] = state;
		return state;
	}

	public void AddAnyTransition(TState to, string? trigger = null, Func<bool>? condition = null)
	{
		_anyTransitions.Add(new Transition<TState>(to, trigger, condition));
	}

	public void AddAnyTrigger(string triggerName, TState to)
	{
		_anyTransitions.Add(new Transition<TState>(to, trigger: triggerName));
	}

	public void AddTransition(TState from, TState to, string? trigger = null, Func<bool>? condition = null)
	{
		if (!_transitions.ContainsKey(from))
			_transitions[from] = new List<Transition<TState>>();

		_transitions[from].Add(new Transition<TState>(to, trigger, condition));
	}

	public void ActivateTrigger(string triggerName)
	{
		_triggers.Add(triggerName);
	}

	public void SetState(TState newState)
	{
		// Try to get the state in one lookup
		if (!_states.TryGetValue(newState, out var nextState))
			throw new Exception($"State {newState} not registered");

		// Already in this state? Do nothing
		if (_currentState != null && EqualityComparer<TState>.Default.Equals(_currentStateKey, newState))
			return;

		// Exit current state
		_currentState?.OnExit?.Invoke();
		OnAnyExit?.Invoke();

		// Set new state
		_currentStateKey = newState;
		_currentState = nextState;

		// Enter new state
		_currentState?.OnEnter?.Invoke();
		OnAnyEnter?.Invoke();
	}

	public void Update()
	{
		if (_currentState == null)
			return;

		Console.WriteLine(_currentStateKey);

		// CHeck any state transitions
		foreach (var t in _anyTransitions)
		{
			bool triggered = t.TriggerName == null || _triggers.Contains(t.TriggerName);
			bool conditionMet = t.CanTransition();

			if (triggered && conditionMet)
			{
				if (t.TriggerName != null)
					_triggers.Remove(t.TriggerName);

				SetState(t.To);
				return;
			}
		}

		// Check transitions
		if (_currentStateKey != null && _transitions.TryGetValue(_currentStateKey, out var transitions))
		{
			foreach (var t in transitions)
			{
				// Trigger-based check: true if no trigger required or trigger is active
				bool triggered = t.TriggerName == null || _triggers.Contains(t.TriggerName);

				// Condition-based check
				bool conditionMet = t.CanTransition();

				if (triggered && conditionMet)
				{
					// Consume the trigger if used
					if (t.TriggerName != null)
						_triggers.Remove(t.TriggerName);

					SetState(t.To);
					return; // exit after first valid transition
				}
			}
		}

		// Update the current state
		_currentState?.OnUpdate?.Invoke();
	}

	public TState CurrentState => _currentStateKey
	?? throw new InvalidOperationException("FSM has no current state set.");
}
