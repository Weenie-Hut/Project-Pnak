using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

namespace Pnak
{
	public class MessageBox : SingletonMono<MessageBox>
	{
		[SerializeField] private UnityEngine.UI.ScrollRect _MessageContainer;
		[SerializeField] private TMPro.TextMeshProUGUI _MessagePrefab;
		[SerializeField] private int _MaxMessages = 50;
		[Tooltip("How long a message will be displayed before being removed")]
		[SerializeField] private float _MessageLifetime = 30;

		private List<TMPro.TextMeshProUGUI> _Messages;

		protected override void Awake()
		{
			base.Awake();
			_Messages = new List<TMPro.TextMeshProUGUI>();

			RPC_ShowMessage("Welcome to Pnak!");
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_ShowMessage(string message, RpcInfo info = default)
		{
			if (!SessionManager.Exists ||
				SessionManager.Instance?.NetworkRunner?.Simulation == null ||
				!SessionManager.Instance.NetworkRunner.Simulation.LocalPlayer.IsValid)
				message = $"[N/A]: {message}";
			else if (SessionManager.Instance.NetworkRunner.Simulation.LocalPlayer == info.Source)
				message = $"You: {message}";
			else
				message = $"{info.Source}: {message}";

			TMPro.TextMeshProUGUI text = Instantiate(_MessagePrefab, _MessageContainer.transform);
			text.text = message;

			// Set the hight of the text to match the text
			text.rectTransform.sizeDelta = new Vector2(text.rectTransform.sizeDelta.x, text.preferredHeight);
			_Messages.Add(text);

			if (_Messages.Count > _MaxMessages)
			{
				Destroy(_Messages[0].gameObject);
				_Messages.RemoveAt(0);
			}
			text.StartCoroutine(CommonCoroutines.CallAfterSeconds(_MessageLifetime, () => Destroy(text.gameObject)));

			// Focus on the last message by scrolling to the bottom
			_MessageContainer.verticalNormalizedPosition = 0;
		}
	}
}
