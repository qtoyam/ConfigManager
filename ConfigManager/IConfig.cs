namespace ConfigManager
{
	public interface IConfig<T> where T : class, new()
	{
		T CurrentValue { get; }
		event ValueChangedHandler<T>? ValueChanged;

		void UpdateValue(Action<T> applyChanges);
		Task UpdateValueAsync(Func<T, Task> applyChanges);

		ConfigValueLocker<T> LockValue();
		Task<ConfigValueLocker<T>> LockValueAsync();
	}
}
