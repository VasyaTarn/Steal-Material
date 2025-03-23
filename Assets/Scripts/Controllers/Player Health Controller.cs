using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;
using UniRx;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using DG.Tweening;

public class PlayerHealthController : NetworkBehaviour
{
    public HealthStats healthStats;
    public NetworkVariable<float> currentHp = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Action<float> OnDamageTaken;

    private readonly Subject<ulong> _onDeathSubject = new Subject<ulong>();
    public IObservable<ulong> OnDeath => _onDeathSubject;
    //public event Action<ulong> OnDeth;

    private Image _healthBarImage;

    private Crosshair _crosshair;

    // Damage effect
    private Vignette _vignette; 
    private float _duration = 0.2f;
    private Tween vignetteTween;

    private void Start()
    {
        healthStats.inResistance = false;
        healthStats.resistancePercentage = 0f;

        if(IsServer)
        {
            SetMaxHpServerRpc();
        }

        if (IsOwner)
        {
            _healthBarImage = UIReferencesManager.Instance.HealthbarImage;
        }

        _crosshair = UIReferencesManager.Instance.Crosshair;

        if (UIReferencesManager.Instance.Vignette.profile.TryGet(out Vignette vignette))
        {
            _vignette = vignette;
        }

        currentHp.OnValueChanged += OnHealthChanged;
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                this.TakeDamage(100f);
            }
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (IsOwner)
        {
            if (oldValue > newValue)
            {
                vignetteTween?.Kill();

                vignetteTween = DOTween.To(() => _vignette.intensity.value,
                                           x => _vignette.intensity.value = x,
                                           0.3f, _duration)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        _vignette.intensity.value = 0;
                        vignetteTween = null;
                    });
            }

        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (_healthBarImage != null)
        {
            _healthBarImage.fillAmount = currentHp.Value / healthStats.maxHp;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMaxHpServerRpc()
    {
        currentHp.Value = healthStats.maxHp;
    }

    public void TakeDamage(float damageNumber)
    {
        if (!healthStats.isImmortal && currentHp.Value > 0)
        {
            _crosshair.AnimateDamageResize();

            SubmitDamageServerRpc(damageNumber);
        }
    }

    public void TakeDamage(float damageNumber, ulong ownerId)
    {
        if (!healthStats.isImmortal && currentHp.Value > 0)
        {
            /*Debug.Log("ID1: " + OwnerClientId); // кто получил урон
            Debug.Log("ID2: " + ownerId); // кто стрельнул*/

            if (ownerId == 0)
            {
                _crosshair.AnimateDamageResize();
            }

            SubmitDamageServerRpc(damageNumber);
        }
    }

    public void TakeDamageByRetaliate(float damageNumber)
    {
        if (!healthStats.isImmortal && currentHp.Value > 0)
        {
            SubmitDamageByRetaliateServerRpc(damageNumber);

            _crosshair.AnimateDamageResize();
        }
    }

    public void Regeneration(float numberHP)
    {
        SubmitRegenerationServerRpc(numberHP);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitDamageServerRpc(float damageNumber)
    {
        DecreaseHealth(damageNumber);

        NotifyDamageTakenClientRpc(damageNumber);

        if (currentHp.Value <= 0)
        {
            DieRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitDamageByRetaliateServerRpc(float damageNumber)
    {
        DecreaseHealth(damageNumber);

        if (currentHp.Value <= 0)
        {
            DieRpc();
        }
    }

    private void DecreaseHealth(float number)
    {
        if (healthStats.inResistance)
        {
            number *= 1 - healthStats.resistancePercentage;
        }

        currentHp.Value -= number;

        /*if (_healthBarImage != null)
        {
            _healthBarImage.fillAmount = currentHp.Value / healthStats.maxHp;
        }*/
    }

    [ClientRpc]
    private void NotifyDamageTakenClientRpc(float damage)
    {
        OnDamageTaken?.Invoke(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitRegenerationServerRpc(float numberHP)
    {
        if(healthStats.maxHp > currentHp.Value)
        {
            currentHp.Value += numberHP;
        }

        //_healthBarImage.fillAmount = currentHp.Value / healthStats.maxHp;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DieRpc()
    {
        if (IsOwner)
        {
            _onDeathSubject.OnNext(OwnerClientId);
        }
        //OnDeth?.Invoke(OwnerClientId);
    }

    public void EnableResistance(float percentage)
    {
        healthStats.inResistance = true;
        healthStats.resistancePercentage = percentage;
    }

    public void DisableResistance()
    {
        healthStats.inResistance = false;
        healthStats.resistancePercentage = 0f;
    }

    public override void OnNetworkDespawn()
    {
        currentHp.OnValueChanged -= OnHealthChanged;
    }    
}
