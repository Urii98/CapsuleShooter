using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class Banner : MonoBehaviour
{
    public TextMeshProUGUI bannerText;
    public AudioSource buttonClickSound;

    public void ShowBanner(string message)
    {
        bannerText.text = message;
        transform.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutBounce);
    }

    public void HideBanner()
    {
        transform.DOLocalMoveY(3000, 0.5f).SetEase(Ease.InBounce);
    }

    public void ShowTemporaryBanner(string message, float duration)
    {
        ShowBanner(message);
        Invoke(nameof(HideBanner), duration);
    }
}
