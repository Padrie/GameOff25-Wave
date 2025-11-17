namespace EasyPeasyFirstPersonController
{
    using UnityEngine;

    [RequireComponent(typeof(FirstPersonController))]
    public class FlashlightMotion : MonoBehaviour
    {
        public Transform flashlightTransform;
        [Range(0f, 0.1f)] public float swayAmount = 0.02f;
        [Range(0f, 10f)] public float swaySmooth = 4f;
        [Range(0f, 5f)] public float swayRotation = 2f;
        [Range(0f, 0.1f)] public float bobVertical = 0.03f;
        [Range(0f, 0.1f)] public float bobHorizontal = 0.02f;
        [Range(0f, 20f)] public float bobSpeed = 10f;
        [Range(0f, 3f)] public float sprintMult = 1.5f;
        [Range(0f, 1f)] public float crouchMult = 0.5f;
        [Range(0f, 2f)] public float slideMult = 1.2f;
        [Range(0f, 30f)] public float sprintTilt = 3f;
        [Range(0f, 30f)] public float slideTilt = 8f;
        [Range(0f, 10f)] public float tiltSpeed = 5f;
        public bool breathingEnabled = true;
        [Range(0f, 0.05f)] public float breathAmt = 0.01f;
        [Range(0f, 2f)] public float breathSpeed = 0.5f;

        private FirstPersonController fpc;
        private Vector3 startPos;
        private Quaternion startRot;
        private float timer, breathTimer;
        private Vector3 swayPos, targetSway;
        private float tilt, tiltVel;
        private CharacterController cc;

        private void Awake()
        {
            fpc = GetComponent<FirstPersonController>();
            cc = fpc.GetComponent<CharacterController>();

            if (flashlightTransform == null)
            {
                Debug.LogWarning("No flashlight assigned");
                enabled = false;
                return;
            }

            startPos = flashlightTransform.localPosition;
            startRot = flashlightTransform.localRotation;
        }

        private void Update()
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            float mult = GetMult();
            targetSway = new Vector3(-mx * swayAmount, -my * swayAmount, 0f) * mult;
            swayPos = Vector3.Lerp(swayPos, targetSway, Time.deltaTime * swaySmooth);

            Vector3 vel = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
            bool moving = vel.magnitude > 0.1f;
            bool grounded = Physics.CheckSphere(fpc.groundCheck.position, fpc.groundDistance, fpc.groundMask, fpc.groundCheckQueryTriggerInteraction);

            if (moving && grounded && !fpc.isSliding)
                timer += Time.deltaTime * bobSpeed * mult;
            else
                timer = Mathf.Lerp(timer, 0f, Time.deltaTime * 5f);

            float targetTilt = fpc.isSprinting ? sprintTilt : (fpc.isSliding ? slideTilt : 0f);
            tilt = Mathf.SmoothDamp(tilt, targetTilt, ref tiltVel, 1f / tiltSpeed);

            if (breathingEnabled)
                breathTimer += Time.deltaTime * breathSpeed;

            float bv = Mathf.Sin(timer) * bobVertical;
            float bh = Mathf.Cos(timer * 0.5f) * bobHorizontal;
            Vector3 bob = new Vector3(bh, bv, 0f);

            Vector3 breath = Vector3.zero;
            if (breathingEnabled)
            {
                float by = Mathf.Sin(breathTimer) * breathAmt;
                breath = new Vector3(0f, by, 0f);
                if (fpc.isSprinting) breath *= 1.5f;
            }

            flashlightTransform.localPosition = startPos + swayPos + bob + breath;

            Vector3 rot = new Vector3(
                swayPos.y * swayRotation * 50f,
                swayPos.x * swayRotation * 50f,
                tilt
            );
            rot.x += Mathf.Sin(timer) * swayRotation;

            Quaternion target = startRot * Quaternion.Euler(rot);
            flashlightTransform.localRotation = Quaternion.Slerp(flashlightTransform.localRotation, target, Time.deltaTime * swaySmooth);
        }

        private float GetMult()
        {
            if (fpc.isSliding) return slideMult;
            if (fpc.isSprinting) return sprintMult;
            if (fpc.isCrouching) return crouchMult;
            return 1f;
        }

        public void ResetFlashlight()
        {
            flashlightTransform.localPosition = startPos;
            flashlightTransform.localRotation = startRot;
            swayPos = Vector3.zero;
            targetSway = Vector3.zero;
            timer = 0f;
            breathTimer = 0f;
            tilt = 0f;
        }
    }
}