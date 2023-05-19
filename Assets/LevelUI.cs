using UnityEngine;
using UnityEngine.UI;

namespace Pnak
{
	public class LevelUI : SingletonMono<LevelUI>
	{
		[SerializeField] public UIProgressBar ShootReloadBar;
		[SerializeField] public UIProgressBar TowerReloadBar;
	}
}