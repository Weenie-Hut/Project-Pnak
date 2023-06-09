using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Character Type")]
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

		public override string Format(string format)
		{
			return base.Format(format)
				.Replace("{name}", AgentPrefab.gameObject.name)
				// .Replace("{description}", CharacterData.Description)
				;
		}
	}
}