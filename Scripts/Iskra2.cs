using UnityEngine;
using Receiver2ModdingKit;
using Iskra2Patch;
using Receiver2;
using Receiver2ModdingKit.CustomSounds;
using System.Linq;
using System;

public class Iskra2 : ModGunScript
{
	public enum BoltState
	{
		Locked,
		Unlocked,
		Unlocking,
		Locking
	}

	// Most sounds come from this addon: https://steamcommunity.com/sharedfiles/filedetails/?id=2393318131&searchtext=arccw+fas+2
	// Firing sound is from Verdun https://store.steampowered.com/app/242860/Verdun/, extracted by Heloft
	public override void InitializeGun()
	{
		pooled_muzzle_flash = ReceiverCoreScript.Instance().gun_prefabs.First(go => go.GetComponent<GunScript>().gun_model == GunModel.Model10).GetComponent<GunScript>().pooled_muzzle_flash;
	}

	public override void AwakeGun()
	{
		var properties = GetComponent<Iskra2WeaponProperties>();

		properties.bolt.transform = transform.Find("bolt");

		properties.bolt.positions[0] = transform.Find("bolt/bolt_in").localPosition;
		properties.bolt.positions[1] = transform.Find("bolt/bolt_out").localPosition;

		properties.bolt_lock.transform = transform.Find("bolt/bolt_body");
		properties.bolt_lock.rotations[0] = transform.Find("bolt/bolt_locked").rotation;
		properties.bolt_lock.rotations[1] = transform.Find("bolt/bolt_unlocked").rotation;

		properties.striker.transform = transform.Find("bolt/striker");
		properties.striker.positions[0] = transform.Find("bolt/striker_in").localPosition;
		properties.striker.positions[1] = transform.Find("bolt/striker_out").localPosition;
		properties.striker.amount = 0;

		properties.bolt_state = BoltState.Locked;

		properties.magazine = GetComponentInChildren<SimpleMagazineScript>();

		PlayerLoadout loadout = ReceiverCoreScript.Instance().CurrentLoadout;
		try
		{
			var equipment = loadout.equipment.Single(eq => eq.internal_name == "Iskra_2_Magazine");

			properties.magazine.SetPersistentData(equipment.persistent_data);
		}
		catch (Exception)
		{
			if (ReceiverCoreScript.Instance().game_mode.GetGameMode() == GameMode.RankingCampaign || ReceiverCoreScript.Instance().game_mode.GetGameMode() == GameMode.Classic)
				properties.magazine.queue_rounds = UnityEngine.Random.Range(0, 6);
		}

		properties.sights.Clear();

		properties.sights.AddRange(
			new SightAttachment[] {
					new SightAttachment(
						"Aperture sight",
						transform.Find("aperture_sight"),
						transform.Find("pose_aim_down_sights_irons"),
						transform.Find("point_bullet_fire_irons")
					),
					new SightAttachment(
						"Notch sight",
						transform.Find("notch_sight"),
						transform.Find("pose_aim_down_sights_irons"),
						transform.Find("point_bullet_fire_irons")
					),
					new SightAttachment(
						"Scope",
						transform.Find("scope"),
						transform.Find("pose_aim_down_sights_scope"),
						transform.Find("point_bullet_fire_scope")
					),
			}
		);

		foreach (var sight in properties.sights)
		{
			if (sight.name != Plugin.sight_type.Value) sight.Disable();
		}
		properties.sights.Single(sight => { return (sight.name == Plugin.sight_type.Value); }).Enable(this);
	}

	public override void UpdateGun()
	{
		var properties = GetComponent<Iskra2WeaponProperties>();

		if (Plugin.sight_type.Value != properties.currentSight.name)
		{
			foreach (var sight in properties.sights)
			{
				if (sight.name != Plugin.sight_type.Value) sight.Disable();
			}
			properties.sights.Single(sight => { return (sight.name == Plugin.sight_type.Value); }).Enable(this);
		}

		properties.bolt.TimeStep(Time.deltaTime);
		properties.bolt_lock.TimeStep(Time.deltaTime);

		yoke_stage = (YokeStage)properties.bolt_state;

		LocalAimHandler handler = LocalAimHandler.player_instance;

		properties.pullingStriker = player_input.GetButton(14);

		if (properties.striker.amount == 1 && _hammer_state != 2)
		{
			if (properties.bolt_state == BoltState.Locked && !properties.press_check) ModAudioManager.PlayOneShotAttached(sound_cocked, properties.striker.transform.gameObject, 0.2f);
			_hammer_state = 2;
		}

		if (player_input.GetButtonDown(11) && properties.bolt_state != BoltState.Unlocked)
		{
			if (properties.bolt_state == BoltState.Locked) ModAudioManager.PlayOneShotAttached(sound_press_check_start, gameObject);
			properties.bolt_state = BoltState.Unlocking;
		}

		if (player_input.GetButtonDown(10) && properties.bolt_state != BoltState.Locked)
		{
			if (properties.bolt_state == BoltState.Unlocked) ModAudioManager.PlayOneShotAttached(sound_slide_released, gameObject);
			properties.bolt_state = BoltState.Locking;
		}

		if (player_input.GetButton(10) && player_input.GetButton(6) && properties.bolt_state == BoltState.Locked)
		{
			if (player_input.GetButtonDown(10) || player_input.GetButtonDown(6)) ModAudioManager.PlayOneShotAttached(sound_press_check_start, gameObject);
			if (properties.bolt.transform.localPosition.z >= properties.striker.positions[1].z && properties.striker.amount != 0)
			{
				properties.striker.asleep = true;
				properties.striker.accel = 0;
				properties.striker.vel = 0;
				properties.striker.amount = Mathf.InverseLerp(properties.striker.positions[1].z, 0, properties.bolt.transform.localPosition.z);
			}

			if (properties.bolt.amount == 0 && properties.bolt_lock.amount != 1)
			{
				properties.bolt_lock.asleep = false;
				properties.bolt_lock.target_amount = 1;
				properties.bolt_lock.accel = 100;
			}
			else
			{
				properties.bolt.asleep = false;
				properties.bolt.target_amount = press_check_amount;
				properties.bolt.accel = 50;
			}

			properties.press_check = true;
		}
		else if (properties.bolt_state == BoltState.Locked)
		{
			if (properties.bolt.transform.localPosition.z >= properties.striker.positions[1].z && properties.bolt_lock.amount == 1 && trigger.amount != 1)
			{
				properties.striker.amount = Mathf.InverseLerp(properties.striker.positions[1].z, 0, properties.bolt.transform.localPosition.z);
			}

			if (properties.bolt.amount > 0)
			{
				properties.bolt.asleep = false;
				properties.bolt.target_amount = 0;
				properties.bolt.accel = -50;
			}
			else
			{
				if (properties.bolt_lock.amount == 1) ModAudioManager.PlayOneShotAttached(sound_press_check_end, gameObject);
				properties.bolt_lock.asleep = false;
				properties.bolt_lock.target_amount = 0;
				properties.bolt_lock.accel = -100;
			}

			if (properties.bolt.amount == 0 && properties.bolt_lock.amount == 0)
			{
				properties.press_check = false;
			}
		}

		if (properties.pullingStriker && properties.bolt_state == BoltState.Locked && !properties.press_check)
		{
			properties.striker.asleep = false;
			properties.striker.target_amount = 1f;
			properties.striker.accel = 85;
		}
		else if (properties.bolt_lock.amount == 0f && properties.striker.amount != 1 && !properties.press_check)
		{
			properties.striker.target_amount = 0f;
			properties.striker.accel = -100;
		}

		if (properties.bolt_state == BoltState.Unlocking)
		{
			if (properties.bolt.amount == 1)
			{
				properties.bolt_state = BoltState.Unlocked;

				if (round_in_chamber != null && round_in_chamber.transform.parent == properties.bolt.transform)
				{
					if (player_input.GetButton(70))
					{
						round_in_chamber.Move(null);
						if (properties.magazine.AddRound(round_in_chamber))
						{
							ModAudioManager.PlayOneShotAttached(sound_insert_mag_empty, round_in_chamber.gameObject, (properties.magazine.num_rounds == properties.magazine.max_rounds) ? 1f : 0.4f);
							round_in_chamber = null;
						}
						else EjectRoundInChamber(0.4f);
					}
					else EjectRoundInChamber(0.4f);
				}
			}
			else
			{
				if (properties.bolt.amount == 0 && properties.bolt_lock.amount != 1)
				{
					properties.bolt_lock.asleep = false;
					properties.bolt_lock.target_amount = 1;
					properties.bolt_lock.accel = 100;
				}
				else
				{
					if (properties.bolt.amount == 0) ModAudioManager.PlayOneShotAttached(sound_slide_back, gameObject);
					properties.bolt.asleep = false;
					properties.bolt.target_amount = 1;
					properties.bolt.accel = 50;
				}

				if (properties.bolt.transform.localPosition.z >= properties.striker.positions[1].z && properties.striker.amount != 0)
				{
					properties.striker.asleep = true;
					properties.striker.accel = 0;
					properties.striker.vel = 0;
					properties.striker.amount = (properties.striker.positions[1].z - properties.bolt.transform.localPosition.z) * (1 / properties.striker.positions[1].z);
				}

				if (properties.bolt.amount > 0.8 && round_in_chamber != null && round_in_chamber.transform.parent == properties.bolt.transform)
				{
					if (player_input.GetButton(70))
					{
						round_in_chamber.Move(null);
						if (properties.magazine.AddRound(round_in_chamber))
						{
							ModAudioManager.PlayOneShotAttached(sound_insert_mag_empty, round_in_chamber.gameObject, (properties.magazine.num_rounds == properties.magazine.max_rounds) ? 1f : 0.4f);
							round_in_chamber = null;
						}
						else EjectRoundInChamber(0.4f);
					}
					else EjectRoundInChamber(0.4f);
				}
			}
		}

		if (properties.bolt_state == BoltState.Locking)
		{
			if (properties.bolt_lock.amount == 0)
			{
				properties.bolt_state = BoltState.Locked;
				properties.striker.asleep = true;
				properties.striker.accel = 0;
			}

			if (properties.bolt.transform.localPosition.z >= properties.striker.positions[1].z && properties.bolt_lock.amount == 1 && trigger.amount != 1)
			{
				properties.striker.amount = (properties.striker.positions[1].z - properties.bolt.transform.localPosition.z) * (1 / properties.striker.positions[1].z);
			}

			if (properties.bolt.amount == 0 && properties.bolt_lock.amount != 0)
			{
				if (properties.bolt_lock.amount == 1) ModAudioManager.PlayOneShotAttached(sound_press_check_end, gameObject);
				properties.bolt_lock.asleep = false;
				properties.bolt_lock.target_amount = 0;
				properties.bolt_lock.accel = -100;
			}
			else
			{
				properties.bolt.asleep = false;
				properties.bolt.target_amount = 0;
				properties.bolt.accel = -50;
			}

			if (properties.bolt.transform.localPosition.z >= transform.Find("magazine/round_top_right").localPosition.y && !round_in_chamber)
			{
				ShellCasingScript round = properties.magazine.RemoveRound();
				properties.magazine.round_insert_amount = 1f - float.Epsilon;

				if (round != null)
				{
					ReceiveRound(round);
					handler.MoveInventoryItem(round, GetComponent<InventorySlot>());
				}
			}
		}

		if (properties.bolt.amount == 0 && round_in_chamber != null)
		{
			round_in_chamber.transform.parent = properties.bolt.transform;
			round_in_chamber.transform.localPosition = Vector3.zero;
			round_in_chamber.transform.localRotation = Quaternion.identity;
		}

		if (player_input.GetButtonDown(70) && properties.bolt_state == BoltState.Unlocked)
		{
			if (round_in_chamber == null && properties.magazine.can_insert_round)
			{
				var bullet = handler.GetBullet(this.cartridge_dimensions);

				if (bullet != null)
				{
					if (properties.magazine.AddRound(bullet))
					{
						ModAudioManager.PlayOneShotAttached(sound_insert_mag_empty, bullet.gameObject);
						ModAudioManager.PlayOneShotAttached("event:/Magazines/1911_mag_bullet_insert_horizontal", bullet.gameObject);
					}
					else handler.ShakeBullets();
				}
				else handler.ShakeBullets();
			}
		}

		if (properties.striker.amount == 1) trigger.asleep = false;

		if (properties.striker.amount == 0)
		{
			properties.decocking = false;
			_hammer_state = 0;
		}

		if (trigger.amount == 1 && properties.bolt_state == BoltState.Locked && properties.striker.amount == 1 && !properties.press_check)
		{
			if (properties.decocking)
			{
				properties.striker.asleep = false;
				properties.striker.accel = -70;
				properties.striker.target_amount = 0;
			}
			else
			{
				if (properties.pullingStriker)
				{
					properties.decocking = true;
				}
				else
				{
					TryFireBullet();
					properties.striker.asleep = false;
					properties.striker.amount = 0;
					properties.striker.target_amount = 0;
					_hammer_state = 0;
					if (!dry_fired)
						transform.Find("pose_aim_down_sights").localPosition += new Vector3(0, 0, -0.04f);
				}
			}
		}

		transform.Find("pose_aim_down_sights").localPosition = Vector3.MoveTowards(transform.Find("pose_aim_down_sights").localPosition, properties.currentSight.ads_pose.localPosition, Time.deltaTime / 3);

		properties.striker.TimeStep(Time.deltaTime);
		properties.striker.UpdateDisplay();
	}
}
