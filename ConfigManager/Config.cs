namespace ConfigManager
{
	internal sealed class Config<T> : IConfig<T>, IDisposable where T : class, new()
	{
		private readonly SemaphoreSlim _ssLocker;
		public Config()
		{
			_ssLocker = new(1, 1);
			CurrentValue = new();
		}

		public T CurrentValue { get; private set; }

		public event ValueChangedHandler<T>? ValueChanged;
		private void RaiseValueChanged() => ValueChanged?.Invoke(CurrentValue);

		public void UpdateValue(Action<T> applyChanges)
		{
			_ssLocker.Wait();
			try
			{
				applyChanges(CurrentValue);
			}
			finally
			{
				_ssLocker.Release();
			}
			RaiseValueChanged();
		}
		public async Task UpdateValueAsync(Func<T, Task> applyChanges)
		{
			await _ssLocker.WaitAsync();
			try
			{
				await applyChanges(CurrentValue);
			}
			finally
			{
				_ssLocker.Release();
			}
			RaiseValueChanged();
		}
		public ConfigValueLocker<T> LockValue() => ConfigValueLocker<T>.Create(_ssLocker, this);
		public Task<ConfigValueLocker<T>> LockValueAsync() => ConfigValueLocker<T>.CreateAsync(_ssLocker, this);
		public void Dispose() => _ssLocker.Dispose();
	}
}
