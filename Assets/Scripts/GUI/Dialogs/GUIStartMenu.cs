using UnityEngine;
using System.Collections;
using Frontiers;
using Frontiers.Data;
using System.Collections.Generic;

namespace Frontiers.GUI
{
		public class GUIStartMenu : GUIEditor <StartMenuResult>, IGUIParentEditor
		{
				public UIButton ContinueButton;
				public UIButton NewButton;
				public UIButton LoadButton;
				public UIButton SaveButton;
				public UIButton OptionsButton;
				public UIButton MultiplayerButton;
				public UIButton QuitButton;
				public UICamera NGUIInput;
				public UILabel ChangeLogLabel;
				public UILabel ChangeLogTitleLabel;

				public override bool ActionCancel(double timeStamp)
				{
						if (EditObject.EnterGameState == FGameState.GamePaused) {
								Finish();
						}
						return true;
				}

				public override void PushEditObjectToNGUIObject()
				{
						if (Profile.Get.Current.HasLastPlayedGame) {
								EditObject.EnableContinueButton = true;
						} else {
								EditObject.EnableContinueButton = false;
						}
						EditObject.EnableOptionsButton = true;
						EditObject.EnableQuitButton = true;
						EditObject.EnableSaveButton = false;

						GameManager.SetState(EditObject.EnterGameState);

						PrimaryInterface.MinimizeAll();
						string changeLogText = string.Empty;
						System.DateTime changeLogTime = new System.DateTime();
						GameData.IO.GetChangeLog(ref changeLogText, ref changeLogTime);
						ChangeLogLabel.text = changeLogText;
						ChangeLogTitleLabel.text = "Most recent changes: " + changeLogTime.ToString();

						//since we're pulling up the start menu, which will cause a pause
						//WHY NOT CALL THE GC?
						System.GC.Collect();

						RefreshButtons();
				}

				public void RefreshButtons()
				{
						switch (EditObject.EnterGameState) {
								case FGameState.GamePaused:
								default:
										ContinueButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										if (Profile.Get.HasSelectedGame) {
												NewButton.SendMessage("SetDisabled", SendMessageOptions.RequireReceiver);
												LoadButton.SendMessage("SetDisabled", SendMessageOptions.RequireReceiver);
										} else {
												NewButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
												LoadButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										}
										SaveButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										OptionsButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										MultiplayerButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										QuitButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										break;

								case FGameState.WaitingForGame:
										ContinueButton.SendMessage("SetDisabled", SendMessageOptions.RequireReceiver);
										NewButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										LoadButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										SaveButton.SendMessage("SetDisabled", SendMessageOptions.RequireReceiver);
										OptionsButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										MultiplayerButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										QuitButton.SendMessage("SetEnabled", SendMessageOptions.RequireReceiver);
										break;
						}
				}

				#region button clicks//TODO make this all coroutines

				public void OnClickContinueButton()
				{
						Finish();
				}

				public void OnClickNewButton()
				{
						GameObject dialog = GUIManager.SpawnNGUIChildEditor(gameObject, GUIManager.Get.NGUINewGameDialog, false);
						NewGameDialogResult editObject = new NewGameDialogResult();
						GUIManager.SendEditObjectToChildEditor <NewGameDialogResult>(new ChildEditorCallback <NewGameDialogResult>(NewGameDialogCallback), dialog, editObject);
						DisableInput();
				}

				protected void NewGameDialogCallback(NewGameDialogResult editObject, IGUIChildEditor <NewGameDialogResult> childEditor)
				{
						if (mDestroyed) {
								return;
						}

						EnableInput();
						if (!editObject.Cancelled) {
								GUIManager.ScaleDownEditor(childEditor.gameObject).Proceed(true);
								GameManager.Load();
								Finish();
						}
				}

				public void OnClickLoadButton()
				{
						if (mDestroyed) {
								return;
						}

						GameObject dialog = GUIManager.SpawnNGUIChildEditor(gameObject, GUIManager.Get.NGUILoadGameDialog, false);
						GUILoadGameDialog loadGameDialog = dialog.GetComponent <GUILoadGameDialog>();
						loadGameDialog.OnLoseFocus += OnLoadGameFinish;
						loadGameDialog.Show();
						DisableInput();
				}

				protected void OnLoadGameFinish()
				{
						if (mDestroyed) {
								return;
						}

						if (GameManager.Is(FGameState.GameLoading)) {
								Finish();
						} else {
								//GUIManager.Get.GetFocus (this);
								EnableInput();
						}
				}

				public void OnClickSaveButton()
				{
						if (mDestroyed) {
								return;
						}

						DisableInput();
						GameObject dialog = GUIManager.SpawnNGUIChildEditor(gameObject, GUIManager.Get.NGUISaveGameDialog, false);
						GUISaveGameDialog saveGameDialog = dialog.GetComponent <GUISaveGameDialog>();
						saveGameDialog.OnLoseFocus += OnSaveGameFinish;
						saveGameDialog.Show();
				}

				protected void OnSaveGameFinish()
				{
						if (mDestroyed) {
								return;
						}

						EnableInput();
				}

				public void OnClickMultiplayerButton()
				{
						GameObject dialog = GUIManager.SpawnNGUIChildEditor(gameObject, GUIManager.Get.NGUIMultiplayerDialog, false);
						MultiplayerSession editObject = new MultiplayerSession();
						GUIManager.SendEditObjectToChildEditor <MultiplayerSession>(new ChildEditorCallback <MultiplayerSession>(MultiplayerDialogCallback), dialog, editObject);
						DisableInput();
				}

				protected void MultiplayerDialogCallback(MultiplayerSession editObject, IGUIChildEditor <MultiplayerSession> childEditor)
				{
						if (mDestroyed) {
								return;
						}

						EnableInput();
						GUIManager.ScaleDownEditor(childEditor.gameObject).Proceed(true);
				}

				public void OnClickQuitButton()
				{
						Application.Quit();
						//GameManager.Quit ();
				}

				public void OnClickOptionsButton()
				{
						if (mDestroyed) {
								return;
						}

						GameObject dialog = GUIManager.SpawnNGUIChildEditor(gameObject, GUIManager.Get.NGUIOptionsDialog, false);
						GUIManager.SendEditObjectToChildEditor <PlayerPreferences>(new ChildEditorCallback <PlayerPreferences>(OptionsDialogCallback), dialog, Profile.Get.CurrentPreferences);
						DisableInput();
				}

				protected void OptionsDialogCallback(PlayerPreferences editObject, IGUIChildEditor <PlayerPreferences> childEditor)
				{
						EnableInput();
						GUIManager.ScaleDownEditor(childEditor.gameObject).Proceed(true);
				}

				protected override void OnFinish()
				{
						GameManager.SetState(EditObject.ExitGameState);
						base.OnFinish();
				}

				#endregion

				protected GameObject mOptions;
		}

		public class StartMenuResult : GenericDialogResult
		{		//TODO clean this up we don't need most of this
				public FGameState EnterGameState;
				public FGameState ExitGameState;
				public bool EnableContinueButton = true;
				public bool EnableNewButton = true;
				public bool EnableSaveButton = true;
				public bool EnableLoadButton = true;
				public bool EnableMultiplayerButton = true;
				public bool EnableOptionsButton = true;
				public bool EnableReturnButton = true;
				public bool EnableQuitButton = true;
				public bool ClickedContinue = false;
				public bool ClickedNew = false;
				public bool	ClickedSave = false;
				public bool	ClickedLoad = false;
				public bool	ClickedOptions = false;
				public bool ClickedMultiplayer = false;
				public bool	ClickedReturn = false;
				public bool	ClickedQuit = false;
		}
}