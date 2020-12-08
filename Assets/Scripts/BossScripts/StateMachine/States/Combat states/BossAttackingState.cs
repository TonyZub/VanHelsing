﻿using RootMotion.Dynamics;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace BeastHunter
{
    public class BossAttackingState : BossBaseState, IDealDamage
    {
        #region Constants

        private const float LOOK_TO_TARGET_SPEED = 1f;
        private const float PART_OF_NONE_ATTACK_TIME_LEFT = 0.15f;
        private const float PART_OF_NONE_ATTACK_TIME_RIGHT = 0.3f;
        private const float ANGLE_SPEED = 150f;
        private const float ANGLE_TARGET_RANGE_MIN = 20f;
        private const float DISTANCE_TO_START_ATTACK = 4f;
        private const float DELAY_HAND_TRIGGER = 0.2f;



        private const int DEFAULT_ATTACK_ID = 0;

        #endregion




        #region Fields

        private Vector3 _lookDirection;
        private Quaternion _toRotation;

        private int _skillId;

        private bool _isDefaultAttackReady = true;
        private bool _isHorizontalFistAttackReady = false;
        private bool _isStompSplashAttackReady = false;
        private bool _isRageOfForestAttackReady = false;
        private bool _isPoisonSporesAttackReady = false;

        private Dictionary<int,int> _readySkillDictionary = new Dictionary<int, int>();

        #endregion


        #region ClassLifeCycle

        public BossAttackingState(BossStateMachine stateMachine) : base(stateMachine)
        {

        }

        #endregion


        #region Methods

        public override void OnAwake()
        {
            _bossModel.LeftHandBehavior.OnFilterHandler += OnHitBoxFilter;
            _bossModel.RightHandBehavior.OnFilterHandler += OnHitBoxFilter;
            _bossModel.LeftHandBehavior.OnTriggerEnterHandler += OnLeftHitBoxHit;
            _bossModel.RightHandBehavior.OnTriggerEnterHandler += OnRightHitBoxHit;
        }

        public override void Initialise()
        {
            CanExit = false;
            CanBeOverriden = true;
            IsBattleState = true;
            base.CurrentAttackTime = 1.5f;
            SetNavMeshAgent(_bossModel.BossTransform.position, 0);

            for (var i = 0; i < _stateMachine.BossSkills._attackStateSkillDictionary.Count; i++)
            {
                _stateMachine.BossSkills._attackStateSkillDictionary[i].SkillCooldown(_stateMachine.BossSkills._attackStateSkillDictionary[i].AttackId, _stateMachine.BossSkills._attackStateSkillDictionary[i].AttackCooldown);
            }
            ChoosingAttackSkill();
        }

        public override void Execute()
        {
            CheckNextMove();
        }

        public override void OnExit()
        {
        }

        public override void OnTearDown()
        {
            _bossModel.LeftHandBehavior.OnFilterHandler -= OnHitBoxFilter;
            _bossModel.RightHandBehavior.OnFilterHandler -= OnHitBoxFilter;
            _bossModel.LeftHandBehavior.OnTriggerEnterHandler -= OnLeftHitBoxHit;
            _bossModel.RightHandBehavior.OnTriggerEnterHandler -= OnRightHitBoxHit;
        }

        private void ChoosingAttackSkill(bool isDefault = false)
        {
            _readySkillDictionary.Clear();
            var j = 0;


            for (var i = 0; i < _stateMachine.BossSkills._attackStateSkillDictionary.Count; i++)
            {
                if (_stateMachine.BossSkills._attackStateSkillDictionary[i].IsAttackReady)
                {
                    if (CheckDistance(_stateMachine.BossSkills._attackStateSkillDictionary[i].AttackRangeMin, _stateMachine.BossSkills._attackStateSkillDictionary[i].AttackRangeMax))
                    {
                        _readySkillDictionary.Add(j, i);
                        j++;
                    }
                }
            }

            if(_readySkillDictionary.Count==0 & _bossData.GetTargetDistance(_bossModel.BossTransform.position, _bossModel.BossCurrentTarget.transform.position)>=DISTANCE_TO_START_ATTACK)
            {
                _stateMachine.SetCurrentStateOverride(BossStatesEnum.Chasing);
                return;
            }

            if (!isDefault & _readySkillDictionary.Count!=0)
            {
                var readyId = UnityEngine.Random.Range(0, _readySkillDictionary.Count);
                _skillId = _readySkillDictionary[readyId];
            }
            else
            {
                _skillId = DEFAULT_ATTACK_ID;
            }

            _stateMachine.BossSkills._attackStateSkillDictionary[_skillId].UseSkill(_skillId);
        }

        private void CheckNextMove()
        {
            if (isAnimationPlay)
            {
                base.CurrentAttackTime = _bossModel.BossAnimator.GetCurrentAnimatorStateInfo(0).length + 0.2f;
                isAnimationPlay = false;
            }

            if (base.CurrentAttackTime > 0)
            {
                base.CurrentAttackTime -= Time.deltaTime;
                
            }
            if (base.CurrentAttackTime <= 0)
            {
                DecideNextMove();
            }
        }

        private bool CheckDirection()
        {
            var isNear = _bossData.CheckIsLookAtTarget(_bossModel.BossTransform.rotation, _mainState.TargetRotation, ANGLE_TARGET_RANGE_MIN);

            if (!isNear)
            {
                CheckTargetDirection();
                TargetOnPlayer();
            }

            return isNear;
        }

        private bool CheckDistance(float distanceRangeMin, float distanceRangeMax)
        {
            if(distanceRangeMin == -1)
            {
                return true;
            }

            bool isNear = _bossData.CheckIsNearTarget(_bossModel.BossTransform.position, _bossModel.BossCurrentTarget.transform.position, distanceRangeMin, distanceRangeMax);
            return isNear;
        }

        private void DecideNextMove()
        {
            SetNavMeshAgent(_bossModel.BossTransform.position, 0);
            _bossModel.LeftHandBehavior.IsInteractable = false;
            _bossModel.RightHandBehavior.IsInteractable = false;
            _bossModel.LeftHandCollider.enabled = false;
            _bossModel.RightHandCollider.enabled = false;

            if (!_bossModel.IsDead && CheckDirection()) //&& CheckDistance())
            {
                ChoosingAttackSkill();
            }
        }

        private void SetNavMeshAgent(Vector3 targetPosition, float speed)
        {
            _bossModel.BossNavAgent.SetDestination(targetPosition);
            _bossModel.BossNavAgent.speed = speed;
        }

        private void TurnOnHitBoxTrigger(WeaponHitBoxBehavior hitBox, float delayTime)
        {
            //TimeRemaining enableHitBox = new TimeRemaining(() => hitBox.IsInteractable = true, _currentAttackTime * delayTime);
            //enableHitBox.AddTimeRemaining(_currentAttackTime * delayTime);
        }

        private void TurnOnHitBoxCollider(Collider hitBox, float delayTime, bool isOn = true)
        {
            TimeRemaining enableHitBox = new TimeRemaining(() => hitBox.enabled = isOn, CurrentAttackTime * delayTime);
            enableHitBox.AddTimeRemaining(CurrentAttackTime * delayTime);
        }


        private bool OnHitBoxFilter(Collider hitedObject)
        {         
            bool isEnemyColliderHit = hitedObject.CompareTag(TagManager.PLAYER);

            if (hitedObject.isTrigger || _stateMachine.CurrentState != _stateMachine.States[BossStatesEnum.Attacking])
            {
                isEnemyColliderHit = false;
            }

            return isEnemyColliderHit;
        }

        private void OnLeftHitBoxHit(ITrigger hitBox, Collider enemy)
        {
            if (hitBox.IsInteractable)
            {
                DealDamage(_stateMachine._context.CharacterModel.PlayerBehavior, Services.SharedInstance.AttackService.
                    CountDamage(_bossModel.WeaponData, _bossModel.BossStats.MainStats, _stateMachine.
                        _context.CharacterModel.PlayerBehavior.Stats));
                hitBox.IsInteractable = false;
            }
        }

        private void OnRightHitBoxHit(ITrigger hitBox, Collider enemy)
        {
            if (hitBox.IsInteractable)
            {
                DealDamage(_stateMachine._context.CharacterModel.PlayerBehavior, Services.SharedInstance.AttackService.
                    CountDamage(_bossModel.WeaponData, _bossModel.BossStats.MainStats, _stateMachine.
                        _context.CharacterModel.PlayerBehavior.Stats));
                hitBox.IsInteractable = false;
            }
        }

        private void CheckTargetDirection()
        {
            Vector3 heading = _bossModel.BossCurrentTarget.transform.position -
                _bossModel.BossTransform.position;

            int directionNumber = _bossData.AngleDirection(
                _bossModel.BossTransform.forward, heading, _bossModel.BossTransform.up);

            switch (directionNumber)
            {
                case -1:
                    _bossModel.BossAnimator.Play("TurningLeftState", 0, 0f);
                    break;
                case 0:
                    _bossModel.BossAnimator.Play("IdleState", 0, 0f);
                    break;
                case 1:
                    _bossModel.BossAnimator.Play("TurningRightState", 0, 0f);
                    break;
                default:
                    _bossModel.BossAnimator.Play("IdleState", 0, 0f);
                    break;
            }
        }


        private void TargetOnPlayer()
        {
            _bossModel.BossTransform.rotation =  _bossData.RotateTo(_bossModel.BossTransform, _bossModel.BossCurrentTarget.transform, ANGLE_SPEED);
        }

        #region IDealDamage

        public void DealDamage(InteractableObjectBehavior enemy, Damage damage)
        {
            if (enemy != null && damage != null)
            {
                enemy.TakeDamageEvent(damage);
            }
        }

        #endregion

        #endregion
    }
}

