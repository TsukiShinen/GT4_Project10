using System;

public class CallbackValue<T>
{
	private T m_CachedValue;
	public Action<T> OnChanged;


	public CallbackValue()
	{
	}

	public CallbackValue(T cachedValue)
	{
		m_CachedValue = cachedValue;
	}

	public T Value
	{
		get => m_CachedValue;
		set
		{
			if (m_CachedValue != null && m_CachedValue.Equals(value))
				return;
			m_CachedValue = value;
			OnChanged?.Invoke(m_CachedValue);
		}
	}

	public void ForceSet(T value)
	{
		m_CachedValue = value;
		OnChanged?.Invoke(m_CachedValue);
	}

	public void SetNoCallback(T value)
	{
		m_CachedValue = value;
	}
}