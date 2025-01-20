using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;

public class PlayerHealthController : NetworkBehaviour
{
    public HealthStats healthStats;
    public NetworkVariable<float> currentHp = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Action<float> OnDamageTaken;

    public Image healthbarSprite;

    private void Start()
    {
        healthStats.inResistance = false;
        healthStats.resistancePercentage = 0f;

        if(IsServer)
        {
            SetMaxHpServerRpc();
        }

        currentHp.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthbarSprite != null)
        {
            healthbarSprite.fillAmount = currentHp.Value / healthStats.maxHp;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetMaxHpServerRpc()
    {
        currentHp.Value = healthStats.maxHp;
    }

    public void TakeDamage(float damageNumber)
    {
        if (!healthStats.isImmortal)
        {
            SubmitDamageServerRpc(damageNumber);
        }
    }

    public void TakeDamageByRetaliate(float damageNumber)
    {
        if (!healthStats.isImmortal)
        {
            SubmitDamageByRetaliateServerRpc(damageNumber);
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

        /*if (currentHp.Value <= 0)
        {
            die();
        }*/
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitDamageByRetaliateServerRpc(float damageNumber)
    {
        DecreaseHealth(damageNumber);

        /*if (currentHp.Value <= 0)
        {
            die();
        }*/
    }

    private void DecreaseHealth(float number)
    {
        if (healthStats.inResistance)
        {
            number *= 1 - healthStats.resistancePercentage;
        }

        currentHp.Value -= number;

        healthbarSprite.fillAmount = currentHp.Value / healthStats.maxHp;
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

        healthbarSprite.fillAmount = currentHp.Value / healthStats.maxHp;
    }

    private void Die()
    {
        Destroy(gameObject);
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
}
