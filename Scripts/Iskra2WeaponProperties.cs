using System.Collections.Generic;
using UnityEngine;
using Receiver2;

namespace Iskra2Patch {
	class Iskra2WeaponProperties : MonoBehaviour{
		public const string apertureSight = "Aperture sight";
		public const string notchSight = "Notch sight";
		public const string scope = "Scope";

		public Iskra2.BoltState bolt_state;

		public bool pullingStriker;
		public bool decocking;
		public bool press_check;
		public SimpleMagazineScript magazine;

		public LinearMover bolt = new LinearMover();
		public RotateMover bolt_lock = new RotateMover();
		public LinearMover striker = new LinearMover();

		public List<SightAttachment> sights = new List<SightAttachment>();
		public SightAttachment currentSight;
	}
}
