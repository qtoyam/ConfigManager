namespace ConfigManager
{
	public delegate void ValueChangedHandler<in T>(T newValue) where T : class, new();


	public interface IFileConfig<T> : IConfig<T> where T : class, new()
	{
		int UnsavedChanges { get; }

		void Save();
		Task SaveAsync();

		void Read();
		Task ReadAsync();
	}
}
