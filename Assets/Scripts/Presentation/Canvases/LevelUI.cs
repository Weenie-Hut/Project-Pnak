using UnityEngine;
using UnityEngine.UI;

namespace Pnak
{
	public class LevelUI : SingletonMono<LevelUI>
	{
		[SerializeField] public FillBar ShootReloadBar;
		[SerializeField] public FillBar TowerReloadBar;
		[SerializeField] public FillBar MPBar;
	}
}