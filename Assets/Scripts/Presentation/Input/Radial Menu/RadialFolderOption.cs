using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "Folder", menuName = "Pnak/Radial/Folder")]
	public class RadialFolderOption : RadialOptionSO
	{
		public RadialOptionSO[] childOptions;

		public override void OnSelect(Interactable _ = null)
		{
			RadialMenu.Instance.Show(childOptions);
		}
	}
}