using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JSI
{
	public class JSIActionGroupSwitch: InternalModule
	{
		[KSPField]
		public string animationName = "";
		[KSPField]
		public string switchTransform = "";
		[KSPField]
		public string actionName = "lights";
		[KSPField]
		public bool reverse = false;
		[KSPField]
		public float customSpeed = 1f;
		[KSPField]
		public string internalLightName = null;
		// Neater.
		private Dictionary<string,KSPActionGroup> grouplist = new Dictionary<string,KSPActionGroup> () { 
			{ "gear",KSPActionGroup.Gear },
			{ "brakes",KSPActionGroup.Brakes },
			{ "lights",KSPActionGroup.Light },
			{ "rcs",KSPActionGroup.RCS },
			{ "sas",KSPActionGroup.SAS },
			{ "abort",KSPActionGroup.Abort },
			{ "stage",KSPActionGroup.Stage },
			{ "custom01",KSPActionGroup.Custom01 },
			{ "custom02",KSPActionGroup.Custom02 },
			{ "custom03",KSPActionGroup.Custom03 },
			{ "custom04",KSPActionGroup.Custom04 },
			{ "custom05",KSPActionGroup.Custom05 },
			{ "custom06",KSPActionGroup.Custom06 },
			{ "custom07",KSPActionGroup.Custom07 },
			{ "custom08",KSPActionGroup.Custom08 },
			{ "custom09",KSPActionGroup.Custom09 },
			{ "custom10",KSPActionGroup.Custom10 }
		};
		// What is it with Xamarin and formatting those dictionaries?...
		private Dictionary<string,bool> customgrouplist = new Dictionary<string,bool> () {
 			{ "intlight",false },
			{ "dummy",false }
		};
		private int actionGroupID;
		private KSPActionGroup actionGroup;
		private Animation anim;
		private bool oldstate = false;
		private bool iscustomaction = false;

		// Persistence for current state variable.
		private JSIInternalPersistence persistence = null;
		private string persistentVarName;

		private Light[] lightobjects;

		public void Start ()
		{
			if (!grouplist.ContainsKey (actionName)) {
				if (!customgrouplist.ContainsKey (actionName)) {
					Debug.Log (String.Format ("JSIActionGroupSwitch: Action \"{0}\" not known, the switch will not work correctly.", actionName));
				} else {
					iscustomaction = true;
				}
			} else {
				actionGroup = grouplist [actionName];
				actionGroupID = BaseAction.GetGroupIndex (actionGroup);

				oldstate = FlightGlobals.ActiveVessel.ActionGroups.groups [actionGroupID];
			}

			// Load our state from storage...
			if (iscustomaction) {
				if (actionName == "intlight") 
					persistentVarName = internalLightName;
				else
					persistentVarName = "switch" + internalProp.propID.ToString ();
				if (persistence == null)
					for (int i=0; i<part.Modules.Count; i++)
						if (part.Modules [i].ClassName == typeof(JSIInternalPersistence).Name)
							persistence = part.Modules [i] as JSIInternalPersistence;
				int retval = persistence.getVar (persistentVarName);
				if (retval > 0 && retval != int.MaxValue)
					oldstate = customgrouplist[actionName] = true;
			}

			// set up the toggle switch
			GameObject buttonObject = base.internalProp.FindModelTransform (switchTransform).gameObject;
			if (buttonObject == null) {
				Debug.Log (String.Format ("JSIActionGroupSwitch: Transform \"{0}\" not found, the switch will not work correctly.", switchTransform));
			}
			buttonHandlerSingular switchToggle = buttonObject.AddComponent<buttonHandlerSingular> ();
			switchToggle.handlerFunction = click;

			// Set up the animation
			anim = base.internalProp.FindModelAnimators (animationName).FirstOrDefault ();
			if (anim != null) {
				anim [animationName].wrapMode = WrapMode.Once;

			} else {
				Debug.Log (String.Format ("JSIActionGroupSwitch: Animation \"{0}\" not found, the switch will not work correctly.", animationName));
			}

			if (oldstate ^ reverse) {
				anim [animationName].speed = float.MaxValue;
				anim [animationName].normalizedTime = 0;

			} else {

				anim [animationName].speed = float.MinValue;
				anim [animationName].normalizedTime = 1;

			}
			anim.Play (animationName);

			// Set up the custom actions..
			switch (actionName) {
			case "intlight":
				lightobjects = this.internalModel.FindModelComponents<Light> ();
				setInternalLights (customgrouplist [actionName]);
				break;
			default:
				break;
			}

		}

		private void setInternalLights (bool value)
		{
			foreach (Light lightobject in lightobjects) {
				// I probably shouldn't filter them every time, but I am getting
				// serously confused by this hierarchy.
				if (lightobject.name == internalLightName)
					lightobject.enabled = value;
			}
		}

		public void click ()
		{
			if (iscustomaction) {
				customgrouplist [actionName] = !customgrouplist [actionName];
				switch (actionName) {
				case "intlight":
					setInternalLights (customgrouplist [actionName]);
					break;
				default:
					break;
				}

				if (persistence != null) {
					persistence.setVar (persistentVarName,customgrouplist[actionName]?1:0);
				}

			} else
				FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup (actionGroup);
		}

		public override void OnUpdate ()
		{
			if (!HighLogic.LoadedSceneIsFlight ||
			    !(vessel == FlightGlobals.ActiveVessel))
				return;

			// Bizarre, but looks like I need to animate things offscreen if I want them in the right condition when camera comes back.
			/*&&
			    (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA ||
			    CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)
			    ))
				return;*/

			bool state = false;
			if (iscustomaction) {
				state = customgrouplist [actionName];
			} else {
				state = FlightGlobals.ActiveVessel.ActionGroups.groups [actionGroupID];
			}

			if (state != oldstate) {
				if (state ^ reverse) {
					anim [animationName].normalizedTime = 0;
					anim [animationName].speed = 1f * customSpeed;
					anim.Play (animationName);
				} else {
					anim [animationName].normalizedTime = 1;
					anim [animationName].speed = -1f * customSpeed;
					anim.Play (animationName);
				}
				oldstate = state;
			}
		}
	}

	public class buttonHandlerSingular:MonoBehaviour
	{
		public delegate void HandlerFunction ();

		public HandlerFunction handlerFunction;

		public void OnMouseDown ()
		{
			handlerFunction ();
		}
	}
}
