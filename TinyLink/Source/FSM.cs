public class State<TState>
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
	public string? Trigger { get; }
	public Func<bool>? Condition { get; }

	public Transition(TState to, string? trigger = null, Func<bool>? condition = null)
	{
		To = to;
		Trigger = trigger;
		Condition = condition;
	}

	public bool CanTransition(HashSet<string> activeTriggers)
	{
		return (Trigger == null || activeTriggers.Contains(Trigger)) && (Condition == null || Condition());
	}
}

public class StateMachine<TState> where TState : Enum
{
	public Action? OnAnyEnter { get; set; } // Invoked OnEnter any state
	public Action? OnAnyExit { get; set; } // Invoked OnExit any state

	private readonly Dictionary<TState, State<TState>> _states = new();
	private readonly List<Transition<TState>> _globalTransitions = new();
	private readonly Dictionary<TState, List<Transition<TState>>> _transitions = new();
	private readonly HashSet<string> _activeTriggers = new();

	private TState? _currentStateKey;
	public TState CurrentState => _currentStateKey ?? throw new InvalidOperationException("FSM has no current state set.");
	private State<TState>? _currentState;

	public State<TState> AddState(TState key, State<TState> state)
	{
		_states[key] = state;
		return state;
	}

	public void AddGlobalTransition(TState to, string? trigger = null, Func<bool>? condition = null)
	{
		_globalTransitions.Add(new Transition<TState>(to, trigger, condition));
	}

	public void AddGlobalConditionTransition(TState to, Func<bool> condition)
	{
		_globalTransitions.Add(new Transition<TState>(to, condition: condition));
	}

	public void AddGlobalTriggerTransition(TState to, string trigger)
	{
		_globalTransitions.Add(new Transition<TState>(to, trigger: trigger));
	}

	public void AddTransition(TState from, TState to, string? trigger = null, Func<bool>? condition = null)
	{
		if (!_transitions.ContainsKey(from))
			_transitions[from] = new List<Transition<TState>>();

		_transitions[from].Add(new Transition<TState>(to, trigger, condition));
	}

	public void AddConditionTransition(TState from, TState to, Func<bool> condition)
	{
		if (!_transitions.ContainsKey(from))
			_transitions[from] = new List<Transition<TState>>();

		_transitions[from].Add(new Transition<TState>(to, condition: condition));
	}

	public void AddTriggerTransition(TState from, TState to, string trigger)
	{
		if (!_transitions.ContainsKey(from))
			_transitions[from] = new List<Transition<TState>>();

		_transitions[from].Add(new Transition<TState>(to, trigger: trigger));
	}

	public void ActivateTrigger(string trigger)
	{
		_activeTriggers.Add(trigger);
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

		// Console.WriteLine(_currentStateKey);

		// Check global state transitions
		foreach (var t in _globalTransitions)
		{
			if (t.CanTransition(_activeTriggers))
			{
				if (t.Trigger != null)
					_activeTriggers.Remove(t.Trigger);

				SetState(t.To);
				return;
			}
		}

		// Check transitions
		if (_currentStateKey != null && _transitions.TryGetValue(_currentStateKey, out var transitions))
		{
			foreach (var t in transitions)
			{
				if (t.CanTransition(_activeTriggers))
				{
					if (t.Trigger != null)
						_activeTriggers.Remove(t.Trigger);

					SetState(t.To);
					return;
				}
			}
		}

		// Update the current state
		_currentState?.OnUpdate?.Invoke();
	}

	public void UpdateCurrentState()
	{
		_currentState?.OnUpdate?.Invoke();
	}
}
