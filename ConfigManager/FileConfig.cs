using System.Text.Json;

namespace ConfigManager
{
	internal sealed class FileConfig<T> : IFileConfig<T>, IDisposable, IAsyncDisposable where T : class, new()
	{
		private readonly FileStream _fileStream;
		private readonly SemaphoreSlim _ssLocker;
		private readonly bool _saveOnDispose;
		private readonly JsonSerializerOptions _jsonOptions;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		internal FileConfig(string fullPath, bool saveOnDispose, FileStreamOptions? fileStreamOptions, JsonSerializerOptions? jsonOptions)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
			_ssLocker = new(1, 1);
			_ssLocker.Wait();
			var dir = Path.GetDirectoryName(fullPath)
				?? throw new DirectoryNotFoundException($"Cant find file directory from path \"{fullPath}\".");
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			fileStreamOptions ??= new()
			{
				Access = FileAccess.ReadWrite,
				BufferSize = 4096,
				Mode = FileMode.OpenOrCreate,
				Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
				Share = FileShare.None
			};
			_fileStream = new(fullPath, fileStreamOptions);
			_jsonOptions = jsonOptions ?? new(JsonSerializerDefaults.General)
			{
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
				NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip,
				WriteIndented = true
			};
			_saveOnDispose = saveOnDispose;
			UnsavedChanges = 0;
			_ssLocker.Release();
		}


		public event ValueChangedHandler<T>? ValueChanged;
		private void RaiseValueChanged() => ValueChanged?.Invoke(CurrentValue);

		public T CurrentValue { get; private set; }

		public int UnsavedChanges { get; private set; }

		public void Read()
		{
			_ssLocker.Wait();
			try
			{
				if (_fileStream.Length == 0)
				{
					CurrentValue = new();
					UnsavedChanges = 1; //to save on dispose if file was empty
				}
				else
				{
					_fileStream.Seek(0, SeekOrigin.Begin);
					CurrentValue = JsonSerializer.Deserialize<T>(_fileStream, _jsonOptions)
						?? throw new JsonException("Can't read json.");
					UnsavedChanges = 0;
				}
			}
			finally
			{
				_ssLocker.Release();
			}
			RaiseValueChanged();
		}
		public async Task ReadAsync()
		{
			await _ssLocker.WaitAsync();
			try
			{
				if (_fileStream.Length == 0)
				{
					CurrentValue = new();
					UnsavedChanges = 1; //to save on dispose if file was empty
				}
				else
				{
					_fileStream.Seek(0, SeekOrigin.Begin);
					CurrentValue = await JsonSerializer.DeserializeAsync<T>(_fileStream, _jsonOptions)
						?? throw new JsonException("Can't read json.");
					UnsavedChanges = 0;
				}
			}
			finally
			{
				_ssLocker.Release();
			}
			RaiseValueChanged();
		}

		public void Save()
		{
			_ssLocker.Wait();
			try
			{
				if (UnsavedChanges == 0) return;
				_fileStream.SetLength(0);
				JsonSerializer.Serialize(_fileStream, CurrentValue, _jsonOptions);
				UnsavedChanges = 0;
			}
			finally
			{
				_ssLocker.Release();
			}
		}
		public async Task SaveAsync()
		{
			await _ssLocker.WaitAsync();
			try
			{
				if (UnsavedChanges == 0) return;
				_fileStream.SetLength(0);
				await JsonSerializer.SerializeAsync(_fileStream, CurrentValue, _jsonOptions);
				UnsavedChanges = 0;
			}
			finally
			{
				_ssLocker.Release();
			}
		}

		public void UpdateValue(Action<T> applyChanges)
		{
			_ssLocker.Wait();
			try
			{
				applyChanges(CurrentValue);
				UnsavedChanges++;
			}
			finally
			{
				_ssLocker.Release();
			}
			RaiseValueChanged();
		}
		public async Task UpdateValueAsync(Func<T,Task> applyChanges)
		{
			await _ssLocker.WaitAsync();
			try
			{
				await applyChanges(CurrentValue);
				UnsavedChanges++;
			}
			finally
			{
				_ssLocker.Release();
			}
			RaiseValueChanged();
		}

		public ConfigValueLocker<T> LockValue() => ConfigValueLocker<T>.Create(_ssLocker, this);
		public Task<ConfigValueLocker<T>> LockValueAsync() => ConfigValueLocker<T>.CreateAsync(_ssLocker, this);
		#region Dispose
		public void Dispose()
		{
			ValueChanged = null;
			if (_saveOnDispose && UnsavedChanges > 0)
			{
				Save();
			}
			_ssLocker.Wait();
			_fileStream.Close();
			_ssLocker.Release();
			_ssLocker.Dispose();
		}

		public async ValueTask DisposeAsync()
		{
			ValueChanged = null;
			if (_saveOnDispose && UnsavedChanges > 0)
			{
				await SaveAsync();
			}
			await _ssLocker.WaitAsync();
			await _fileStream.DisposeAsync();
			_ssLocker.Release();
			_ssLocker.Dispose();
		}


		#endregion //Dispose
	}
}
