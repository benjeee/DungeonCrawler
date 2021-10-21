using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class ThirdPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 2.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 5.335f;
		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.50f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.28f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 70.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -30.0f;
		[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
		public float CameraAngleOverride = 0.0f;
		[Tooltip("For locking the camera position on all axis")]
		public bool LockCameraPosition = false;

        [Header("OurStuff")]
        [SerializeField] Transform cameraTransform;
        [SerializeField] float hookshotRange;
        [SerializeField] int playerType; //0 == hookshot, 1 = wall jump
		[SerializeField] public AudioClip ShootHookClip;
		[SerializeField] public AudioClip BounceClip;
		[SerializeField] public AudioClip FootClip;

        private Transform _shape;
        private GameObject _hookshotLine;

		// cinemachine
		private float _cinemachineTargetYaw;
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _animationBlend;
		private float _targetRotation = 0.0f;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		// animation IDs
		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;

		private Animator _animator;
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		[SerializeField] private GameObject _mainCamera;
        [SerializeField] Material reticuleMaterial;
        [SerializeField] float reticuleSize;
        [SerializeField] float hookshotMoveSpeed = 35.0f;
        private Vector3 hookshotHookPos;
        private bool hookshotQueued;
        private bool inHookshot;
        private bool hasTarget;
        private float targetDecayTime;
        private Vector3 hookshotTargetPos;
        [SerializeField] private float hookshotDetachDist;
        [SerializeField] private float walljumpCooldown;
        [SerializeField] private Transform hookshotSourcePos;
        private float timeSinceWallJump;
        private bool inWalljump;

        private bool isJumpingOffWall;
        private Vector3 wallJumpVelocity;
        private float timeJumpingOffWall = 0.0f;

		private float timeSinceDeath = 100.0f;
		private float timeSinceFootClip = 0.0f;


		private const float _threshold = 0.01f;

		private bool _hasAnimator;

		private GameObject _audioSpot;

		private void Awake()
		{
		}

		private void Start()
		{
			_audioSpot = GameObject.Find("audiospot");
			Debug.Log(_audioSpot);
			_hasAnimator = TryGetComponent(out _animator);
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();

			AssignAnimationIDs();

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

        private void Update()
        {
			timeSinceDeath += Time.deltaTime;
            _hasAnimator = TryGetComponent(out _animator);


            if (!isJumpingOffWall)
            {
                JumpAndGravity();
            }

            if (!inWalljump)
            {
                GroundedCheck();
            }

            if (isJumpingOffWall)
            {
                _controller.Move(wallJumpVelocity * Time.deltaTime);

                timeJumpingOffWall += Time.deltaTime;
                if (timeJumpingOffWall > 0.5f)
                {
                    wallJumpVelocity = wallJumpVelocity * 0.9f;
                    if (Vector3.Magnitude(wallJumpVelocity) < 0.01f)
                    {
                        isJumpingOffWall = false;
                    }
                }
            }

            Ability1();

            if (!inHookshot && !inWalljump && !isJumpingOffWall && (timeSinceDeath > .5f))
            {
                Move();
            }
        }

        private void renderReticule(Vector3 pos, float radius)
        {
            if (!_shape)
            { // if shape doesn't exist yet, create it
                _shape = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                Destroy(_shape.GetComponent<Collider>()); // no collider, please!
                _shape.GetComponent<Renderer>().material = reticuleMaterial; // assign the selected material to it
            }
            Vector3 scale; // calculate desired scale
            scale.x = radius; // width = capsule diameter
            scale.y = radius; // capsule height
            scale.z = radius; // volume length
            _shape.localScale = scale; // set the rectangular volume size
                                       // set volume position and rotation
            _shape.position = pos;
            _shape.GetComponent<Renderer>().enabled = true; // show it
        }

        private void renderHookshotLine()
        {
            LineRenderer lRend = null;
            if (!_hookshotLine)
            {
                _hookshotLine = new GameObject("hookshotline");
                lRend = _hookshotLine.AddComponent<LineRenderer>();
                lRend.material = reticuleMaterial;
            }
            if (!lRend)
            {
                lRend = _hookshotLine.GetComponent<LineRenderer>();
            }
            lRend.startWidth = 0.05f;
            lRend.endWidth = 0.05f;
            lRend.SetPosition(0, hookshotSourcePos.position);
            lRend.SetPosition(1, hookshotHookPos);
        }

        private void doHookshot()
        {
            if (hookshotQueued)
            {
                Vector3 targetVelocity = Vector3.Normalize(hookshotTargetPos - hookshotHookPos) * hookshotMoveSpeed;
                hookshotHookPos = hookshotHookPos + targetVelocity * Time.deltaTime;
                if (Vector3.Distance(hookshotHookPos, hookshotTargetPos) < 0.3)
                {
                    inHookshot = true;
                    hookshotQueued = false;
                }
            }
            else if (inHookshot)
            {
                //check if we are close enough to target pos
                float dist = Vector3.Distance(hookshotSourcePos.position, hookshotTargetPos);
                if (dist < hookshotDetachDist)
                {
                    Destroy(_hookshotLine.gameObject);
                    inHookshot = false;
                    return;
                }

                //update player velocity each frame while in hookshot
                float scalar = 20.0f;

                Vector3 currVelocity = _controller.velocity;
                Vector3 targetVelocityNormal = Vector3.Normalize(hookshotTargetPos - hookshotSourcePos.position);
                Vector3 targetVelocity = targetVelocityNormal * scalar;

                Vector3 newVelocity = currVelocity + ((targetVelocity - currVelocity) * 0.2f);
                _controller.Move(newVelocity * Time.deltaTime);
            }

            Vector3 targetDir = hookshotTargetPos - transform.position;
            Vector3 rot = Vector3.RotateTowards(transform.forward, targetDir, 1.0f, 0.0f);
            rot.y = 0.0f;
            transform.rotation = Quaternion.LookRotation(rot);
            renderHookshotLine();
        }

        private void Ability1()
        {
            Vector3 fwd = cameraTransform.TransformDirection(Vector3.forward);

            bool hookshotPressed = false;
            if (_input.ability1)
            {		
                if (playerType == 0)
                {
					_audioSpot.transform.position = transform.position;
					AudioSource.PlayClipAtPoint(ShootHookClip, _audioSpot.transform.position);

                    hookshotPressed = true;
                }
            }

            if (inHookshot || hookshotQueued)
            {
                if (hookshotPressed)
                {
                    Destroy(_hookshotLine.gameObject);
                    hookshotQueued = false;
                    inHookshot = false;
                    hasTarget = false;
                }
                else
                {
                    doHookshot();
                }
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(cameraTransform.position, fwd, out hit, hookshotRange))
                {
                    if (hit.collider.gameObject.GetComponent<HookshotTarget>())
                    {
                        hasTarget = true;
                        targetDecayTime = 0.5f;
                        hookshotTargetPos = hit.point;
                        renderReticule(hit.point, reticuleSize);
                    }
                }
                else
                {
                    targetDecayTime -= Time.deltaTime;
                    if (targetDecayTime < 0.0f)
                    {
                        if (_shape)
                        {
                            Destroy(_shape.gameObject);
                        }
                        hasTarget = false;
                    }
                }
            }

            if (hookshotPressed && hasTarget)
            {
                hookshotQueued = true;
                hookshotHookPos = transform.position;
                //inHookshot = true;
            }

            _input.ability1 = false;

        }

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void AssignAnimationIDs()
		{
			_animIDSpeed = Animator.StringToHash("Speed");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDJump = Animator.StringToHash("Jump");
			_animIDFreeFall = Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

			// update animator if using character
			if (_hasAnimator)
			{
				_animator.SetBool(_animIDGrounded, Grounded);
			}
		}

		private void CameraRotation()
		{
			// if there is an input and camera position is not fixed
			if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
			{
				_cinemachineTargetYaw += _input.look.x * Time.deltaTime;
				_cinemachineTargetPitch += _input.look.y * Time.deltaTime;
			}

			// clamp our rotations so our values are limited 360 degrees
			_cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Cinemachine will follow this target
			CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
		}

		private void Move()
		{
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = SprintSpeed;// _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}
			_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
				float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

				// rotate to face input direction relative to camera position
				transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
			}

			timeSinceFootClip += Time.deltaTime;
			if (Grounded && _speed > 1.0f && timeSinceFootClip > 0.35f) 
			{
				timeSinceFootClip = 0.0f;
				_audioSpot.transform.position = transform.position;
				AudioSource.PlayClipAtPoint(FootClip, _audioSpot.transform.position);
			}


			Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

			// move the player
			_controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

			// update animator if using character
			if (_hasAnimator)
			{
				_animator.SetFloat(_animIDSpeed, _animationBlend);
				_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
			}
		}

		private void JumpAndGravity()
		{
			if (Grounded || inHookshot || inWalljump)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// update animator if using character
				if (_hasAnimator)
				{
					_animator.SetBool(_animIDJump, false);
					_animator.SetBool(_animIDFreeFall, false);
				}

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
                    if (!inHookshot)
                    {
                        if (!inWalljump)
                        {
                            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        }
                        else
                        {
                            inWalljump = false;
                            timeSinceWallJump = 0.0f;
                            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        }
                    }


					// update animator if using character
					if (_hasAnimator)
					{
						_animator.SetBool(_animIDJump, true);
					}
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}
				else
				{
					// update animator if using character
					if (_hasAnimator)
					{
						_animator.SetBool(_animIDFreeFall, true);
					}
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
                if (!inWalljump)
                {
                    _verticalVelocity += Gravity * Time.deltaTime;
                }
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;
			
			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.GetComponent<Walljumpable>())
            {
                if (!Grounded)
                {
                    Vector3 currVelocity = _controller.velocity;
                    if (Vector3.Dot(Vector3.Normalize(currVelocity), hit.normal) < -0.2f)
                    {
						_audioSpot.transform.position = transform.position;
						AudioSource.PlayClipAtPoint(BounceClip, _audioSpot.transform.position);

                        isJumpingOffWall = true;
                        timeJumpingOffWall = 0.0f;
                        wallJumpVelocity = Vector3.Reflect(currVelocity, -hit.normal);
                    }
                }

                if (!Grounded)
                {
                    //inWalljump = true;
                }
            }
        }

		void OnTriggerEnter(Collider collider)
		{
			KillVolume killVolume = collider.gameObject.GetComponent<KillVolume>();
			if (killVolume)
			{
				timeSinceDeath = 0.0f;
				_controller.enabled = false;
				_controller.transform.position = killVolume.respawnPosition.position;
				_controller.enabled = true;
				_speed = 0.0f;
			}
		}
    }
}