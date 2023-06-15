using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial/Character Option")]
	public class CharacterTypeRadialOption : RadialOptionSO
	{
		[Required] public PlayerAgent AgentPrefab;

		public override void OnSelect(Interactable _ = null)
		{
			if (Player.LocalPlayer == null)
			{
				Debug.LogError("CharacterTypeRadialOption.OnSelect: Player.LocalPlayer is null! This should only be called once the local player has been instantiated.");
				return;
			}

			int characterIndex = System.Array.IndexOf(GameManager.Instance.Characters, AgentPrefab);
			Player.LocalPlayer.RPC_ChangePlayerAgent((byte)(characterIndex));
		}

		

		public override string Format(string format, Interactable interactable = null)
		{
			return base.Format(format, interactable)
				.Replace("{name}", AgentPrefab.gameObject.name)
				// .Replace("{description}", CharacterData.Description)
				;
		}
		
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (AgentPrefab == null)
				return;

			if (string.IsNullOrEmpty(TitleFormat))
				TitleFormat = "{name}";

			if (string.IsNullOrEmpty(DescriptionFormat))
				DescriptionFormat = "{name}";

			base.OnValidate();
		}
#endif
	}
}