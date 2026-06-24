using NUnit.Framework;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools; // Requerido para pruebas en PlayMode

public class RhythmGameTests
{
    private GameObject playerObject;
    private PlayerHealth healthComponent;
    private GameObject gameManagerObject;
    private GameManager managerComponent;

    // El Setup se ejecuta ANTES de cada prueba
    [SetUp]
    public void Setup()
    {
        playerObject = new GameObject("TestPlayer");
        healthComponent = playerObject.AddComponent<PlayerHealth>();

        gameManagerObject = new GameObject("TestGameManager");
        managerComponent = gameManagerObject.AddComponent<GameManager>();
    }

    // El TearDown limpia la memoria DESPUÉS de cada prueba
    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(gameManagerObject);
    }

    // PRUEBA 1: Verificar el daño
    [Test]
    public void PlayerHealth_RecibeDano_ReduceSaludCorrectamente()
    {
        healthComponent.maxHealth = 100;
        healthComponent.ResetearSalud(); // Inicia en 100

        healthComponent.TakeDamage(15);

        Assert.AreEqual(85, healthComponent.currentHealth, "La salud debió quedar en 85 tras recibir 15 de daño.");
    }

    // PRUEBA 2: Verificar la curación completa
    [Test]
    public void PlayerHealth_ResetearSalud_DevuelveVidaAlMaximo()
    {
        healthComponent.maxHealth = 100;
        healthComponent.currentHealth = 10; // Casi muerto

        healthComponent.ResetearSalud();

        Assert.AreEqual(100, healthComponent.currentHealth, "El carro debió curarse al 100%.");
    }

    // PRUEBA 3: Verificar que el combo no rompa el límite
    [Test]
    public void GameManager_MultiplicadorCombo_NoSuperaTopeMaximo()
    {
        managerComponent.scoreMultiplier = 1;
        managerComponent.currentComboCount = 0;

        // Simulamos recoger muchas anillas seguidas
        for (int i = 0; i < 40; i++)
        {
            managerComponent.AgregarAnillaCombo();
        }

        Assert.AreEqual(10, managerComponent.scoreMultiplier, "El multiplicador de puntos no debe superar 10x.");
    }
}