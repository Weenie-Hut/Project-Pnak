using UnityEngine;

namespace Pnak
{
	public enum InputBehaviourType
	{
		[Tooltip("Behaviour will use player input if exists, and will use automatic otherwise.")]
		Any = 0,
		[Tooltip("Only player input will be used. Nothing will shoot if player input does not exist.")]
		PlayerInputOnly = 1,
		[Tooltip("Player input will be ignored. Useful if player input is being used somewhere else on the same object. (If input is never being used elsewhere, this is the same as Any)")]
		AutomaticOnly = 2
	}
}