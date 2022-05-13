namespace ConfigManager
{
	public interface IConfig<T> where T : class, new()
	{
		/// <summary>
		/// Get current value. Not thread-safe.
		/// </summary>
		T CurrentValue { get; }

		/// <summary>
		/// Occurs when <see cref="CurrentValue"/> changed.
		/// </summary>
		event ValueChangedHandler<T>? ValueChanged;

		/// <summary>
		/// Update <see cref="CurrentValue"/>. Thread-safe.
		/// </summary>
		/// <param name="applyChanges"></param>
		void UpdateValue(Action<T> applyChanges);

		/// <summary>
		/// Asynchronous update <see cref="CurrentValue"/>. Thread-safe.
		/// </summary>
		/// <param name="applyChanges"></param>
		/// <returns></returns>
		Task UpdateValueAsync(Func<T, Task> applyChanges);

		/// <summary>
		/// Lock <see cref="CurrentValue"/>, so it will be unchanged untill <see cref="ConfigValueLocker{T}"/> disposed.
		/// </summary>
		/// <returns></returns>
		ConfigValueLocker<T> LockValue();

		/// <summary>
		/// Asynchronous lock <see cref="CurrentValue"/>, so it will be unchanged untill <see cref="ConfigValueLocker{T}"/> disposed.
		/// </summary>
		/// <returns></returns>
		Task<ConfigValueLocker<T>> LockValueAsync();
	}
}
