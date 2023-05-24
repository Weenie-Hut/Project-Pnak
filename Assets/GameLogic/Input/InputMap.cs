namespace Pnak.Input
{
	// TODO: This might be needed for filtering actions based on map, if filtering requires matching multiple maps (thus also not matching one or more).
	// public enum InputMapMask
	// {
	// }

	public enum InputMap
	{
		/// <summary> No input. Note that UI input is never disabled, as it is controlled by an EventSystem, not the PlayerInput component. </summary>
		None = 0,
		/// <summary> Gameplay input. </summary>
		Gameplay = 1,
		/// <summary> Menu input. </summary>
		Menu = 2,
	}

	public static class InputMapExtensions
	{
		private static string[] _inputMapNames = new string[]
		{
			"None",
			"Gameplay",
			"Menu",
		};

		public static string Name(this InputMap inputMap)
		{
			return _inputMapNames[(int)inputMap];
		}
	}
}