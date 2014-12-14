using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Frontiers;
using Frontiers.World.Gameplay;
using Frontiers.GUI;

namespace Frontiers.World
{
		public class WorldItemUsable : SpawnOptionsList
		{		//spawns interactive lists specifically for worlditems
				//automatically gathers up all the appropriate skills & all that
				public List <string> PlacementOptions = new List <string>();
				public bool IncludeInteract = true;

				protected override bool PlayerFocus {
						get {
								return Item.HasPlayerFocus;
						}
						set {
								return;
						}
				}

				public WorldItem Item;

				public override void Awake()
				{
						Item = gameObject.GetComponent <WorldItem>();
						base.Awake();
						FunctionTarget = gameObject;
						FunctionName = "OnPlayerUseWorldItem";
						SecondaryFunctionName = "OnPlayerUseWorldItemSecondary";
						RequirePlayerFocus = true;
						RequirePlayerTrigger = false;
						RequireManualEnable = true;
						ShowDoppleganger = true;
				}

				public override void OnGainPlayerFocus()
				{
						PlayerFocus = true;
						CheckIfConditionsAreMet();
				}

				public void Close()
				{
						Debug.Log("Attempting to close...");
						if (IsInUse) {
								Debug.Log("Finishing");
								mChildEditor.Finish();
						}
				}

				public override bool TryToSpawn(bool forceSpawn, out GUIOptionListDialog childEditor)
				{
						childEditor = null;
						if (IsInUse || Item.Is(WIMode.RemovedFromGame)) {
								return false;
						}
						MessageType = Item.DisplayName;
						if (base.TryToSpawn(forceSpawn, out childEditor)) {
								if (ShowDoppleganger) {
										mChildEditor.DopplegangerProps.CopyFrom(Item);
										mChildEditor.RefreshDoppleganger();
								} else {
										mChildEditor.DopplegangerProps.Clear();
								}
								return true;
						}
						return false;
				}

				public override bool TryToSpawn()
				{
						if (IsInUse || !Item.Is(WILoadState.Initialized)) {
								return false;
						}

						Item.Usable.MessageType = Item.StackName;
						if (base.TryToSpawn()) {
								if (ShowDoppleganger) {
										mChildEditor.DopplegangerProps.CopyFrom(Item);
										mChildEditor.RefreshDoppleganger();
								} else {
										mChildEditor.DopplegangerProps.Clear();
								}
								return true;
						}
						return false;
				}

				public override void PopulateDialogOptions()
				{
						mOptions.Clear();
						mMessage.Clear();
						Item.PopulateOptionsList(mOptions, mMessage, IncludeInteract);

						mLastAssociatedSkillList = Skills.Get.SkillsAssociatedWith(Item);
						for (int i = 0; i < mLastAssociatedSkillList.Count; i++) {
								Skill skill = mLastAssociatedSkillList[i];
								mOptions.Add(skill.GetListOption(Item));
						}
				}

				public virtual void OnPlayerUseWorldItemSecondary(object secondaryResult)
				{	//this is where we handle skills
						OptionsListDialogResult dialogResult = secondaryResult as OptionsListDialogResult;
						for (int i = 0; i < mLastAssociatedSkillList.Count; i++) {
								Skill skill = mLastAssociatedSkillList[i];
								if (skill.name == dialogResult.SecondaryResult) {
										//SKILL USE
										skill.Use(Item, dialogResult.SecondaryResultFlavor);
										break;
								}
						}
				}

				protected List <Skill> mLastAssociatedSkillList	= new List <Skill>();
		}
}