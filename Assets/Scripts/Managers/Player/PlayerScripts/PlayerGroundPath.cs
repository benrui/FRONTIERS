using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Frontiers;
using Frontiers.World;
using Frontiers.GUI;

namespace Frontiers
{
	public class PlayerGroundPath : MonoBehaviour
	{
		//this script is responsible for the glowy path that follows the player around
		public SplineAnimatorClosestPoint Follower;
		public Transform FollowerTarget;
		public Spline spline;
		public SplineMesh PathMesh;
		public MeshRenderer PathRenderer;
		public List <SplineNode> Nodes = new List <SplineNode>();
		float PathFollowSpeed = 0.125f;
		float DistanceBetweenNodes = 3.0f;
		float WaveAmount = 0.25f;
		float TimeModifier = 1.0f;
		public float PlayerDistanceFromPath;

		public void Awake()
		{
			gameObject.layer = Globals.LayerNumScenery;
			for (int i = 0; i < Globals.GroundPathFollowerNodes; i++) {
				SplineNode node = gameObject.CreateChild("Node").gameObject.AddComponent <SplineNode>();
				node.transform.position = new Vector3(i, i, i);
				node.tension = 1f;
				node.normal = Vector3.up;
				Nodes.Add(node);
			}
			PlayerDistanceFromPath = 0f;
		}

		public void Start()
		{
			spline = gameObject.AddComponent <Spline>();
			spline.updateMode = Spline.UpdateMode.EveryFrame;
			spline.enabled = false;

			PathMesh = gameObject.AddComponent <SplineMesh>();
			PathMesh.spline = spline;
			PathMesh.startBaseMesh = Meshes.Get.GroundPathPlane;
			PathMesh.baseMesh = Meshes.Get.GroundPathPlane;
			PathMesh.endBaseMesh = Meshes.Get.GroundPathPlane;
			PathMesh.highAccuracy = false;
			PathMesh.segmentCount = 25;
			PathMesh.uvMode = SplineMesh.UVMode.InterpolateU;
			PathMesh.uvScale = Vector2.one;

			PathRenderer = gameObject.AddComponent <MeshRenderer>();
			PathRenderer.sharedMaterials = new Material [] { Mats.Get.WorldPathGroundMaterial };
			PathRenderer.enabled = false;

			foreach (SplineNode node in Nodes) {
				spline.splineNodesArray.Add(node);
			}

			Follower = gameObject.CreateChild("Follower").gameObject.AddComponent <SplineAnimatorClosestPoint>();
			Follower.iterations = 1;
			Follower.offset = 0f;
			Follower.target = FollowerTarget;

			mTerrainHit.groundedHeight = 1.0f;
		}

		public void FixedUpdate()
		{
			/*if (!Follower.enabled) {
				FollowerTarget.position = Player.Local.HeadPosition;
			}
			Follower.enabled = !Follower.enabled;*/
		}

		public void Update()
		{
			if (!GameManager.Is(FGameState.InGame)) {
				return;
			}

			if (!Paths.HasActivePath) {
				if (spline.enabled) {
					spline.enabled = false;
					PathMesh.enabled = false;
					PathMesh.renderer.enabled = false;
					PathMesh.updateMode = SplineMesh.UpdateMode.DontUpdate;
					Follower.enabled = false;
					Follower.gameObject.SetActive(false);
					mCurrentColor = Color.black;
				}
				return;
			} else if (Paths.HasActivePath) {
				if (!spline.enabled) {
					spline.enabled = true;
					PathMesh.enabled = true;
					PathMesh.renderer.enabled = true;
					PathMesh.updateMode = SplineMesh.UpdateMode.WhenSplineChanged;
					Follower.enabled = true;
					Follower.gameObject.SetActive(true);
					Follower.spline = Paths.ActivePath.spline;
				}
			}

			PathFollowSpeed = 0.125f;
			DistanceBetweenNodes = 2.0f;
			WaveAmount = 0.25f;
			TimeModifier = 1.0f;

			switch (TravelManager.Get.State) {
				case FastTravelState.None:
					//only check this when we're not fast-traveling
					if (PlayerDistanceFromPath > Globals.PathStrayDistanceInMeters) {
						if (mTimeAwayFromPath > Globals.PathStrayMinTimeInSeconds) {
							if (mTimeAwayFromPath > Globals.PathStrayMaxTimeInSeconds) {
								GUIManager.PostWarning("Stopped following path");
								Paths.ClearActivePath();
								mTimeAwayFromPath = 0.0f;
								return;
							}
						}
						mTimeAwayFromPath += WorldClock.ARTDeltaTime;
					} else {
						mTimeAwayFromPath = 0.0f;
					}
										//see what color we're supposed to be
					if (Paths.IsEvaluating) {
						mTargetColor = Color.Lerp(Colors.Get.PathEvaluatingColor1, Colors.Get.PathEvaluatingColor2, Mathf.Abs(Mathf.Sin((float)(WorldClock.RealTime * 2))));
					} else {
						float meters = Paths.ActivePath.MetersFromPosition(Follower.transform.position);
						mTargetColor = Colors.GetColorFromWorldPathDifficulty(Paths.ActivePath.SegmentFromMeters(meters).Difficulty);
					}
					break;

				case FastTravelState.WaitingForNextChoice:
					mTargetColor = Color.black;
					break;

				default:
					break;
			}

			if (Player.Local.State.IsHijacked) {
				PathFollowSpeed = 1.0f;
				DistanceBetweenNodes = 4.0f;
				WaveAmount = 0.05f;
			}		

			PlayerDistanceFromPath = Vector3.Distance(Follower.transform.position, FollowerTarget.position) * Globals.InGameUnitsToMeters;

			mTargetColor.a = 0.2f * Profile.Get.CurrentPreferences.Immersion.PathGlowIntensity;
			mCurrentColor = Color.Lerp(mCurrentColor, mTargetColor, 0.125f);
			PathMesh.renderer.material.SetColor("_TintColor", mCurrentColor);

			mGroundPathSmoothTarget = Mathf.Lerp(mGroundPathSmoothTarget, Follower.param, PathFollowSpeed);
			mGroundActivePathSmoothTarget = Mathf.Lerp(mGroundActivePathSmoothTarget, Follower.param, PathFollowSpeed);

			mTotalLength = Paths.ActivePath.LengthInMeters;
			mNormalizedTargetLength = (Nodes.Count * DistanceBetweenNodes) / mTotalLength;
			mNormalizedExtent = (mNormalizedTargetLength / 2.0f);
			mNormalizedMidPoint = mGroundActivePathSmoothTarget;
			mNormalizedStartPoint = mNormalizedMidPoint - mNormalizedExtent;
			mNormalizedEndPoint = mNormalizedMidPoint + mNormalizedExtent;
			mNormalizedDistanceBetweenNodes = DistanceBetweenNodes / mTotalLength;
			mNormalizedDistance = mNormalizedStartPoint;

			//spline.splineNodesArray.Clear();

			int activeNodes = 0;

			mTerrainHit.overhangHeight = 2f;
			mTerrainHit.groundedHeight = 2f;
			mTerrainHit.ignoreWorldItems = true;
			for (int i = 0; i < Nodes.Count; i++) {
				if ((mNormalizedStartPoint >= 0.0f && mNormalizedStartPoint < 1.0f) || (mNormalizedEndPoint >= 0.0f && mNormalizedEndPoint < 1.0f)) {
					Nodes[i].gameObject.SetActive(true);
					mNodeWorldMapPosition = Paths.ActivePath.spline.GetPositionOnSpline(mNormalizedDistance);
					mNormalizedDistance += mNormalizedDistanceBetweenNodes;

					mTerrainHit.feetPosition = mNodeWorldMapPosition;
					mTerrainHit.feetPosition.y = GameWorld.Get.TerrainHeightAtInGamePosition(ref mTerrainHit)
					+ 1.0f
					+ (Mathf.Sin(((float)(WorldClock.RealTime * TimeModifier)) + (mTotalLength * mNormalizedDistance)) * WaveAmount);
					Nodes[i].transform.position = mTerrainHit.feetPosition;

					//spline.splineNodesArray.Add(Nodes[i]);
					
					if (i == Nodes.Count / 2) {
						mInterceptorPosition = mTerrainHit.feetPosition;
					}
					activeNodes++;
				} else {
					Nodes[i].gameObject.SetActive(false);
				}
			}
		}

		protected float mTotalLength;
		protected float mNormalizedTargetLength;
		protected float mNormalizedExtent;
		protected float mNormalizedMidPoint;
		protected float mNormalizedStartPoint;
		protected float mNormalizedEndPoint;
		protected float mNormalizedDistanceBetweenNodes;
		protected float mNormalizedDistance;
		protected Vector3 mNodeWorldMapPosition;
		protected Vector3 mInterceptorPosition;
		protected GameWorld.TerrainHeightSearch mTerrainHit;
		protected float mGroundPathSmoothTarget = 0.0f;
		protected float mGroundActivePathSmoothTarget = 0.0f;
		protected Color mTargetColor = Color.white;
		protected Color mCurrentColor = Color.white;
		protected bool mHasNotifiedOfStraying = false;
		protected double mTimeAwayFromPath = 0.0f;
	}
}