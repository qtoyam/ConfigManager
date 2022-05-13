namespace ConfigManager
{
	public sealed class ConfigValueLocker<T> : IDisposable where T : class, new()
	{
		private readonly SemaphoreSlim _ssLocker;
		public T Value { get; }

		private ConfigValueLocker(SemaphoreSlim locker, T value)
		{
			_ssLocker = locker;
			Value = value;
		}

		internal static ConfigValueLocker<T> Create(SemaphoreSlim locker, IConfig<T> config)
		{
			locker.Wait();
			return new ConfigValueLocker<T>(locker, config.CurrentValue);
		}
		internal static async Task<ConfigValueLocker<T>> CreateAsync(SemaphoreSlim locker, IConfig<T> config)
		{
			await locker.WaitAsync();
			return new ConfigValueLocker<T>(locker, config.CurrentValue);
		}

		public void Dispose()
		{
			_ssLocker.Release();
		}
	}
}
