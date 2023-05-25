using System;
using System.Collections.Generic;
using UnityEngine;
using Pnak.Input;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class RadialMenu : SingletonMono<RadialMenu>
	{
		/// <summary>
		/// The option indices to use based on the number of options in the menu.
		/// Option Indices:
		/// 7  0  1
		/// 6     2
		/// 5  4  3
		/// </summary>
		private static readonly int[][] UseOptionsByCount = new int[][] {
			new int[] { 0 },
			new int[] { 6, 2 },
			new int[] { 0, 5, 3 },
			new int[] { 6, 0, 2, 4 },
			new int[] { 6, 0, 5, 3, 2 },
			new int[] { 6, 0, 5, 3, 2, 4 },
			new int[] { 7, 0, 1, 6, 2, 5, 3 },
			new int[] { 7, 0, 1, 6, 2, 5, 3, 4 },
		};

		[SerializeField] private GameObject RadialMenuUI;
		[SerializeField] private RadialMenuOption[] UIOptions;
		[SerializeField] private Sprite MoreOptionsIcon;


		private Interactable interactable;
		private List<RadialOptionSO[]> optionStack;
		private RadialOptionSO[] currentOptions => optionStack[optionStack.Count - 1];
		private bool[] affordability;
		private int hoveredOption = -1;

		protected override void Awake()
		{
			base.Awake();

			optionStack = new List<RadialOptionSO[]>();
			affordability = new bool[UIOptions.Length];
			if (RadialMenuUI == null) RadialMenuUI = gameObject;
			InputCallbackSystem.CreateInputCallbacks(this, false);
		}

		public void Show(RadialOptionSO[] options, Interactable interactable = null)
		{
			if (options.Length == 0)
			{
				UnityEngine.Debug.LogError("RadialMenu.Show: options.Length == 0");
				return;
			}

			if (interactable != null)
			{
				if (interactable != this.interactable && (optionStack.Count > 0 || this.interactable != null))
					UnityEngine.Debug.LogWarning($"RadialMenu.Show: Interactable changed while menu was open (stacks: {optionStack.Count}) => Old: {this.interactable}, New: {interactable}.");

				this.interactable = interactable;
			}

			if (options.Length > UseOptionsByCount.Length)
			{
				var temp = new RadialOptionSO[UseOptionsByCount.Length];
				Array.Copy(options, temp, temp.Length - 1);
				temp[temp.Length - 1] = PackOptionsIntoFolders(options, temp.Length - 1);
				options = temp;
			}

			optionStack.Add(options);

			RadialMenuUI.SetActive(true);
			_ShowOptions(options);

			GameInput.Instance.SetInputMap(InputMap.Menu);
			InputCallbackSystem.RegisterInputCallbacks(this);
		}

		private void Update()
		{
			if (optionStack.Count == 0)
				return;

			float angle = GameInput.Instance.InputData.AimAngle;

			// Using the angle, find the option that is being hovered over.
			int closestOption = -1;
			float closestAngle = float.MaxValue;

			var inUse = UseOptionsByCount[currentOptions.Length - 1];
			for (int i = 0; i < inUse.Length; i++)
			{
				int index = inUse[i];
				float optionAngle = (360f / UIOptions.Length) * index + 90f;
				float angleDifference = Mathf.Abs(Mathf.DeltaAngle(angle, optionAngle));

				if (angleDifference < closestAngle)
				{
					closestOption = index;
					closestAngle = angleDifference;
				}

				bool afford = currentOptions[i].IsSelectable(interactable);
				if (afford != affordability[index])
				{
					affordability[index] = afford;
					UIOptions[index].UpdateAffordability(afford);
				}
			}

			if (hoveredOption != closestOption)
			{
				if (hoveredOption != -1)
					UIOptions[hoveredOption].Hovered = false;

				hoveredOption = closestOption;

				if (hoveredOption != -1)
					UIOptions[hoveredOption].Hovered = true;
			}
		}

		[InputActionTriggered(ActionNames.Back, InputStateFilters.PreformedThisFrame)]
		private void _Back(InputAction.CallbackContext context)
		{
			if (optionStack.Count == 0)
				return;

			optionStack.RemoveAt(optionStack.Count - 1);

			if (optionStack.Count == 0)
				return;

			_ShowOptions(currentOptions);
		}

		[InputActionTriggered(ActionNames.Confirm, InputStateFilters.PreformedThisFrame)]
		private void _Confirm(InputAction.CallbackContext context)
		{
			if (optionStack.Count == 0)
				return;

			int optionIndex = Array.IndexOf(UseOptionsByCount[currentOptions.Length - 1], hoveredOption);

			if (optionIndex == -1)
				return;

			RadialOptionSO option = currentOptions[optionIndex];
			option.OnSelect(interactable);

			if (hoveredOption != -1)
			{
				UIOptions[hoveredOption].Hovered = false;
				hoveredOption = -1;
			}

			ClearAndHide();
		}

		[InputActionTriggered(ActionNames.ToggleMenu, InputStateFilters.PreformedThisFrame)]
		private void _ToggleMenu(InputAction.CallbackContext context)
		{
			if (optionStack.Count == 0)
				return;

			ClearAndHide();
		}

		private void ClearAndHide()
		{
			optionStack.Clear();
			RadialMenuUI.SetActive(false);
			interactable = null;

			if (hoveredOption != -1)
			{
				UIOptions[hoveredOption].Hovered = false;
				hoveredOption = -1;
			}

			GameInput.Instance.SetInputMap(InputMap.Gameplay);
			InputCallbackSystem.UnregisterInputCallbacks(this);
		}


		public RadialOptionSO PackOptionsIntoFolders(RadialOptionSO[] options, int startIndex)
		{
			int length = options.Length - startIndex;

			if (length == 0)
				return null;

			if (length == 1)
				return options[startIndex];

			int min = Mathf.Min(UseOptionsByCount.Length, length);

			RadialFolderOption folder = ScriptableObject.CreateInstance<RadialFolderOption>();
			folder.Title = "More Options";
			folder.Icon = MoreOptionsIcon;
			folder.Description = "Open more options.";

			folder.childOptions = new RadialOptionSO[min];

			for (int i = 0; i < min - 1; i++)
				folder.childOptions[i] = options[startIndex++];

			folder.childOptions[min - 1] = PackOptionsIntoFolders(options, startIndex);

			return folder;
		}

		private void _ShowOptions(RadialOptionSO[] options)
		{
			int[] useOptions = UseOptionsByCount[options.Length - 1];

			List<int> indicesNotSet = new List<int>();
			for (int i = 0; i < UIOptions.Length; i++)
				indicesNotSet.Add(i);

			for (int i = 0; i < useOptions.Length; i++)
			{
				int index = useOptions[i];
				UIOptions[index].SetData(options[i]);
				indicesNotSet.Remove(index);

#if UNITY_EDITOR
				if (!Application.isPlaying)
					affordability[index] = true;
				else
#endif
				affordability[index] = options[i].IsSelectable(interactable);

				UIOptions[index].UpdateAffordability(affordability[index]);
			}

			foreach (int index in indicesNotSet)
			{
				UIOptions[index].SetData(null);
				affordability[index] = false;
			}
		}

#if UNITY_EDITOR
		[Header("Editor")]
		[SerializeField] private RadialOptionSO[] _PreviewData;
		[SerializeField] private float _UIOptionRadius = 300f;
		[SerializeField, HideInInspector] private int lastCount = -1;
		private void OnValidate()
		{
			if (UIOptions == null)
				return;

			if (lastCount != UIOptions.Length)
			{
				
				UnityEditor.EditorApplication.delayCall += () => {
					if (UIOptions == null)
						return;
					lastCount = UIOptions.Length;

					for (int i = 0; i < UIOptions.Length; i++)
					{
						float angle = (360f / UIOptions.Length) * i;
						Vector3 pos = Quaternion.Euler(0f, 0f, angle) * Vector3.up * _UIOptionRadius;
						if (UIOptions[i]?.RectTransform != null)
							UIOptions[i].RectTransform.anchoredPosition = pos;
					}
				};
				return;
			}

			if (_PreviewData != null &&
				_PreviewData.Length > 0 &&
				UIOptions.Length == UseOptionsByCount.Length &&
				_PreviewData.Length <= UseOptionsByCount.Length)
				_ShowOptions(_PreviewData);
		}

		// Draw an error gismo if the options.length does not match UseOptionsByCount.Length
		private void OnDrawGizmos()
		{
			if (UIOptions == null)
				return;

			if (UIOptions.Length != UseOptionsByCount.Length || _PreviewData.Length > UseOptionsByCount.Length)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(transform.position, 100f);
			}
		}
#endif
	}
}