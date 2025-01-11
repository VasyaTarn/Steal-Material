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

    //private StatusEffectsController statusEffectsController;

    private void Start()
    {
        //statusEffectsController = GetComponent<StatusEffectsController>();

        healthStats.inResistance = false;
        healthStats.resistancePercentage = 0f;

        if(IsServer)
        {
            setMaxHpServerRpc();
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
    private void setMaxHpServerRpc()
    {
        currentHp.Value = healthStats.maxHp;
    }

    public void takeDamage(float damageNumber)
    {
        if (!healthStats.isImmortal)
        {
            submitDamageServerRpc(damageNumber);
        }
    }

    public void takeDamageByRetaliate(float damageNumber)
    {
        if (!healthStats.isImmortal)
        {
            submitDamageByRetaliateServerRpc(damageNumber);
        }
    }

    public void regeneration(float numberHP)
    {
        submitRegenerationServerRpc(numberHP);
    }

    [ServerRpc(RequireOwnership = false)]
    private void submitDamageServerRpc(float damageNumber)
    {
        decreaseHealth(damageNumber);

        notifyDamageTakenClientRpc(damageNumber);

        /*if (currentHp.Value <= 0)
        {
            die();
        }*/
    }

    [ServerRpc(RequireOwnership = false)]
    private void submitDamageByRetaliateServerRpc(float damageNumber)
    {
        decreaseHealth(damageNumber);

        /*if (currentHp.Value <= 0)
        {
            die();
        }*/
    }

    private void decreaseHealth(float number)
    {
        if (healthStats.inResistance)
        {
            number *= 1 - healthStats.resistancePercentage;
        }

        currentHp.Value -= number;

        healthbarSprite.fillAmount = currentHp.Value / healthStats.maxHp;
    }

    [ClientRpc]
    private void notifyDamageTakenClientRpc(float damage)
    {
        OnDamageTaken?.Invoke(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void submitRegenerationServerRpc(float numberHP)
    {
        if(healthStats.maxHp > currentHp.Value)
        {
            currentHp.Value += numberHP;
        }

        healthbarSprite.fillAmount = currentHp.Value / healthStats.maxHp;
    }

    private void die()
    {
        Destroy(gameObject);
    }

    public void enableResistance(float percentage)
    {
        healthStats.inResistance = true;
        healthStats.resistancePercentage = percentage;
    }

    public void disableResistance()
    {
        healthStats.inResistance = false;
        healthStats.resistancePercentage = 0f;
    }
}
