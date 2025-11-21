using UnityEngine;
using DG.Tweening;

public class ItemMotion : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float swayAmount;
    [SerializeField] private float swaySpeed = 1;
    [SerializeField] private float bobAmount;
    [SerializeField] private float bobSpeed;
    [SerializeField] private float velocityMultiplierStrength = 1f;

    private Vector3 initialPosition;
    private float swayTime;
    private float bobTime;
    private float randomSeed;

    private void Start()
    {
        initialPosition = transform.localPosition;
        randomSeed = Random.Range(0f, 100f);
    }

    private void Awake()
    {
        characterController = FindFirstObjectByType<CharacterController>();
    }

    private void Update()
    {
        if (characterController.velocity.magnitude > 0.1f)
        {
            //velocity markiplier
            float velocityMultiplier = 1f + (characterController.velocity.magnitude * velocityMultiplierStrength);

            swayTime += Time.deltaTime * swaySpeed * velocityMultiplier;
            bobTime += Time.deltaTime * bobSpeed * velocityMultiplier;

            float swayX = Mathf.Sin(swayTime + randomSeed) * swayAmount * velocityMultiplier;
            float bobY = Mathf.Sin((bobTime + randomSeed) * 2f) * bobAmount * velocityMultiplier;

            Vector3 targetPosition = initialPosition + new Vector3(swayX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
        }
        else
        {
            transform.DOLocalMove(initialPosition, 0.3f).SetEase(Ease.OutQuad);
        }
    }
}