using UnityEngine;
using UnityEngine.UI;

namespace Pnak
{
	public class LevelUI : SingletonMono<LevelUI>
	{
		[SerializeField] public UIFillBar ShootReloadBar;
		[SerializeField] public UIFillBar TowerReloadBar;
		[SerializeField] public UIFillBar MPBar;
	}
}