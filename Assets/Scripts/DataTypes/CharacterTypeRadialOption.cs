using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Character Type")]
	public class CharacterTypeRadialOption : RadialOptionSO
	{
		public CharacterData CharacterData;

		public override void OnSelect(Interactable _ = null)
		{
			if (Player.LocalPlayer == null)
			{
				Debug.LogError("CharacterTypeRadialOption.OnSelect: Player.LocalPlayer is null! This should only be called once the local player has been instantiated.");
				return;
			}

			int characterIndex = System.Array.IndexOf(GameManager.Instance.Characters, CharacterData);
			Player.LocalPlayer.RPC_SetCharacterType((byte)(characterIndex + 1));
		}

		private void OnValidate()
		{
			if (CharacterData == null)
				return;

			if (string.IsNullOrEmpty(Title))
				Title = CharacterData.Name;

			if (Icon == null)
				Icon = CharacterData.Sprite;
		}
	}
}