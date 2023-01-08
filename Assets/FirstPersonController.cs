using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private float health;
        [SerializeField] private float m_ForwardWalkSpeed;
        [SerializeField] private float m_BackwardWalkSpeed;
        [SerializeField] private float m_CrouchingSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpHeight;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private float m_CrouchHeight;
        [SerializeField] private float m_ScopedSensitivity;
        [SerializeField] private LayerMask m_WhatIsGround;
        [SerializeField] private GameObject m_CrouchCheck;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FOVKickRun = new FOVKick();
        [SerializeField] private FOVKick m_FOVKickCrouch = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;
        [SerializeField] private AudioClip m_JumpSound;
        [SerializeField] private AudioClip m_LandSound;
        [SerializeField] private Manager manager;

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private float m_JumpSpeed;
        private AudioSource m_AudioSource;
        private bool m_IsWalking;
        private bool m_IsBackwards;
        private bool m_IsCrouching;
        private float m_OriginalHeight;
        private bool m_Crouch = false;
        private bool waswalking;
        private bool wascrouching;
        private bool m_BlockedByCollider = false;
        private float m_NormalSensitivity;
        private bool paused = false;
        bool skipMove = false;

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FOVKickRun.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
            m_MouseLook.Init(transform, m_Camera.transform);
            m_OriginalHeight = m_CharacterController.height;
            m_NormalSensitivity = m_MouseLook.GetSensitivity();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R) && !paused)
            {
                manager.Pause();
                paused = true;
                m_MouseLook.SetSensitivity(0);
            }
            else if (Input.GetKeyDown(KeyCode.R) && paused)
            {
                manager.Resume();
                paused = false;
                m_MouseLook.SetSensitivity(m_NormalSensitivity);
            }

            RotateView();

            if (health <= 0 && Input.GetButtonDown("Cancel"))
            {
                manager.Restart();
                Time.timeScale = 1f;
            }
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }

        private void CheckCrouch()
        {
            if (m_Crouch == true)
            {
                m_CharacterController.height = m_CrouchHeight;
            }
            else
            {
                m_CharacterController.height = m_OriginalHeight;
            }
        }

        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }

        public void SkipMove()
        {
            skipMove = true;
        }
        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);

            if (!skipMove)
            {
                Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

                RaycastHit hitInfo;
                Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                                   m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

                m_MoveDir.x = desiredMove.x * speed;
                m_MoveDir.z = desiredMove.z * speed;


                if (m_CharacterController.isGrounded)
                {
                    m_MoveDir.y = -m_StickToGroundForce;

                    if (m_Jump)
                    {
                        m_JumpSpeed = Mathf.Sqrt(Physics.gravity.y * m_GravityMultiplier * -2 * m_JumpHeight);
                        m_MoveDir.y = m_JumpSpeed;
                        PlayJumpSound();
                        m_Jump = false;
                        m_Jumping = true;
                    }
                }
                else
                {
                    m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
                }

                m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

                ProgressStepCycle(speed);
                UpdateCameraPosition(speed);

                m_MouseLook.UpdateCursorLock();
            }
        }
        public void TakeDamage(float amount)
        {
            health -= amount;
            Debug.Log(health);
            if (health <= 0)
            {
                manager.GameOver();
                m_MouseLook.SetSensitivity(0);
                Time.timeScale = 0f;
            }
        }

        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }

        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }

            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);

            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }

        public void SetCrouch(bool value)
        {
            m_BlockedByCollider = value;
        }

        private void GetInput(out float speed)
        {
            if (!skipMove)
            {
                float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
                float vertical = CrossPlatformInputManager.GetAxis("Vertical");

                waswalking = m_IsWalking;
                wascrouching = m_IsCrouching;

                m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
                m_IsCrouching = Input.GetKey(KeyCode.LeftControl);
                m_IsBackwards = Input.GetAxis("Vertical") < 0;

                if (m_IsCrouching || m_BlockedByCollider)
                {
                    m_Crouch = true;
                    m_IsCrouching = true;
                    CheckCrouch();
                }
                else
                {
                    m_Crouch = false;
                    CheckCrouch();
                }

                speed = m_IsCrouching ? m_CrouchingSpeed : m_IsBackwards ? m_BackwardWalkSpeed : m_IsWalking ? m_ForwardWalkSpeed : m_RunSpeed;
                if (speed == m_RunSpeed && Input.GetMouseButton(1))
                {
                    speed = m_ForwardWalkSpeed;
                }
                m_Input = new Vector2(horizontal, vertical);
                if (m_Input.sqrMagnitude > 1)
                {
                    m_Input.Normalize();
                }
                if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0 && !m_IsBackwards && !Input.GetMouseButton(1))
                {
                    StopAllCoroutines();
                    StartCoroutine(!m_IsWalking ? m_FOVKickRun.FOVKickUp() : m_FOVKickRun.FOVKickDown());
                }
            }
            else
            {
                speed = 0;
                skipMove = false;
            }
        }

        public void Scope()
        {
            m_ForwardWalkSpeed = m_ForwardWalkSpeed / 2;
            m_BackwardWalkSpeed = m_BackwardWalkSpeed / 2;
            m_CrouchingSpeed = m_CrouchingSpeed / 2;
            m_RunSpeed = m_RunSpeed / 2;
            m_MouseLook.SetSensitivity(m_ScopedSensitivity);
        }

        public void UnScope()
        {
            m_ForwardWalkSpeed = m_ForwardWalkSpeed * 2;
            m_BackwardWalkSpeed = m_BackwardWalkSpeed * 2;
            m_CrouchingSpeed = m_CrouchingSpeed * 2;
            m_RunSpeed = m_RunSpeed * 2;
            m_MouseLook.SetSensitivity(m_NormalSensitivity);
        }

        private void RotateView()
        {
            m_MouseLook.LookRotation(transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.15f, hit.point, ForceMode.Impulse);
        }
    }
}
