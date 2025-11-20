using UnityEngine;
using DG.Tweening;

public class ItemMotion : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float swayAmount;
    [SerializeField] private float swaySpeed = 1;
    [SerializeField] private float bobAmount;
    [SerializeField] private float bobSpeed;

    private Vector3 initialPosition;
    private float swayTime;
    private float bobTime;

    private void Start()
    {
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        if (characterController.velocity.magnitude > 0.1f)
        {
            swayTime += Time.deltaTime * swaySpeed;
            bobTime += Time.deltaTime * bobSpeed;

            float swayX = Mathf.Sin(swayTime) * swayAmount;
            float bobY = Mathf.Sin(bobTime * 2f) * bobAmount;

            Vector3 targetPosition = initialPosition + new Vector3(swayX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
        }
        else
        {
            transform.DOLocalMove(initialPosition, 0.3f).SetEase(Ease.OutQuad);
        }
    }
}