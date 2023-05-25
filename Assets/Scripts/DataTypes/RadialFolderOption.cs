using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Folder")]
	public class RadialFolderOption : RadialOptionSO
	{
		public RadialOptionSO[] childOptions;

		public override void OnSelect(Interactable _ = null)
		{
			RadialMenu.Instance.Show(childOptions);
		}
	}
}