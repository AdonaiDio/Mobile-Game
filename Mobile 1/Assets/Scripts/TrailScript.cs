using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TrailScript : MonoBehaviour
{
    // Script para definir um trail. Seguimentos e Conexões entre Frames.
    //[HideInInspector]
    public FrameScript frame_A;
    //[HideInInspector]
    public List<TrailUnit> trail = new List<TrailUnit>();
    //[HideInInspector]
    List<TrailUnit> reverseTrail = new List<TrailUnit>();
    //[HideInInspector]
    public FrameScript frame_B;
    //[HideInInspector]
    public int trailId;

    public GameObject energyPrefab;
    private bool isFrameAShooting;
    private bool isFrameBShooting;
    private bool isShootingDuel;
    private float shootingCountdown = 5f;

    //StateMachine
    private FiniteStateMachine fsm;

    private enum TrailState
    {
        Neutral,
        Charge,
        Duel,
        Canceling //estado transitório. Serve para impedir ações sobrepostas antes do termino da transição
    }

    private TrailState currentTrailState;
    private Faction.FactionName factionName;
    private Vector3 gizmoPoint;
    private Ray ray;

    public TextMeshProUGUI debugStateTxt;////////////////////////////////////////////////

    private void Awake()
    {
        FulfillTrail();
        SetAdjacentFrames();
    }


    void Start()
    {
        //// Setting FiniteStateMachine ////
        // States
        FsmState neutral = new FsmState(ShowState, null, null);
        FsmState charge = new FsmState(ShowState, null, null);
        FsmState duel = new FsmState(ShowState, null, null);

        //TouchControls controls = new TouchControls();
        // Conditions <bool>
        Func<bool> neutralCondition = () => currentTrailState == TrailState.Neutral;
        Func<bool> chargingCondition = () => currentTrailState == TrailState.Charge;
        Func<bool> duelingCondition = () => currentTrailState == TrailState.Duel;

        // Transitions
        neutral.When(chargingCondition, charge, null);
        charge.When(neutralCondition, neutral, null);
        charge.When(duelingCondition, duel, null);
        duel.When(chargingCondition, charge, null);

        //Construct FSM
        fsm = new FiniteStateMachine(neutral);
        fsm.AddStates(neutral, charge, duel);

        //// Normal Start() ////
        currentTrailState = TrailState.Neutral;
        factionName = Faction.FactionName.Neutral;

        //Resetar a facção dos trilhos para Neutral
        //Set units IDs
        int countId = 0;
        foreach (TrailUnit unit in trail)
        {
            countId += 1;

            unit.UpdateToFaction(Faction.FactionName.Neutral);

            string stringId = (trailId+"-"+countId);
            unit.unitId = stringId;
        }
        foreach (TrailUnit u in trail)
        {
            reverseTrail.Insert(0, u);
        }
        //reverseTrail.Reverse();
        isFrameAShooting = false;
        isFrameBShooting = false;
        isShootingDuel = false;

    }

    void OnEnable()
    {
        Events.onConnect.AddListener(ConnectionHandler);
        Events.onCancelConnection.AddListener(CancelConnectionHandler);
    }

    void OnDisable()
    {
        Events.onConnect.RemoveListener(ConnectionHandler);
        Events.onCancelConnection.RemoveListener(CancelConnectionHandler);
    }
    private void Update()
    {
        fsm.ExecuteActions(fsm.Tick());
        debugStateTxt.text = currentTrailState.ToString(); /////////////////

        if (isShootingDuel)
        {
            shootingCountdown -= Time.deltaTime;
            if (shootingCountdown <= 0f)
                StartCoroutine(ShootingEnergyPulseDuel());
        }else{
            StopCoroutine(ShootingEnergyPulseDuel());
        }
        if (isFrameAShooting)
        {
            shootingCountdown -= Time.deltaTime;
            if(shootingCountdown <= 0f)
                StartCoroutine(ShootEnergyPulseA());
        }else { 
            StopCoroutine(ShootEnergyPulseA());
        }
        if (isFrameBShooting)
        {
            shootingCountdown -= Time.deltaTime;
            if (shootingCountdown <= 0f)
                StartCoroutine(ShootEnergyPulseB());
        }else{
            StopCoroutine(ShootEnergyPulseB());
        }
    }
    private IEnumerator ShootingEnergyPulseDuel()
    {
        int countUnits = 0;
        shootingCountdown = 5f;
        yield return new WaitForSeconds(.5f); //segura alguns frames para impedir que ShootEnergyPulse seja executado mais de 1 vez
        if (TrailState.Duel == currentTrailState)
        {
            GameObject energyPulseA = Instantiate(energyPrefab, trail[0].transform.position + new Vector3(0, 0, -1f), Quaternion.identity);
            energyPulseA.GetComponent<EnergyPulse>().energyPulseWaitTime = frame_A.thisPixeltron.GetComponent<Pixeltron>()._energyPulseWaitTime;
            energyPulseA.GetComponent<EnergyPulse>().energyPulseTimeToMove = frame_A.thisPixeltron.GetComponent<Pixeltron>()._energyPulseTimeToMove;

            GameObject energyPulseB = Instantiate(energyPrefab, reverseTrail[0].transform.position + new Vector3(0, 0, -1f), Quaternion.identity);
            energyPulseB.GetComponent<EnergyPulse>().energyPulseWaitTime = frame_B.thisPixeltron.GetComponent<Pixeltron>()._energyPulseWaitTime;
            energyPulseB.GetComponent<EnergyPulse>().energyPulseTimeToMove = frame_B.thisPixeltron.GetComponent<Pixeltron>()._energyPulseTimeToMove;

            //foreach (TrailUnit unit in trail)
            for(int i=0; i<trail.Count; i++)
            {
                TrailUnit unit = trail[i];
                TrailUnit reverseUnit = reverseTrail[i];

                countUnits += 1;

                LeanTween.move(energyPulseA, unit.transform.position + new Vector3(0, 0, -1f), energyPulseA.GetComponent<EnergyPulse>().energyPulseTimeToMove);
                LeanTween.move(energyPulseB, reverseUnit.transform.position + new Vector3(0, 0, -1f), energyPulseB.GetComponent<EnergyPulse>().energyPulseTimeToMove);
                yield return new WaitUntil(() => (unit.transform.position.x - .05f <= energyPulseA.transform.position.x && energyPulseA.transform.position.x <= unit.transform.position.x + .05f)
                                              && (unit.transform.position.y - .05f <= energyPulseA.transform.position.y && energyPulseA.transform.position.y <= unit.transform.position.y + .05f));
                //yield return new WaitUntil(() => (reverseUnit.transform.position.x - .05f <= energyPulseB.transform.position.x && energyPulseB.transform.position.x <= reverseUnit.transform.position.x + .05f)
                //                              && (reverseUnit.transform.position.y - .05f <= energyPulseB.transform.position.y && energyPulseB.transform.position.y <= reverseUnit.transform.position.y + .05f));

                //se a energia chegar até a ultima unit aliada +0,25 offset pra qualquer direção, então Destroy e contabiliza dano, break;
                if (TrailState.Neutral == currentTrailState)
                {
                    isFrameAShooting = false;
                    isFrameBShooting = false;
                    isShootingDuel = false;
                    Destroy(energyPulseA);
                    Destroy(energyPulseB);
                    break;
                }
                else if (TrailState.Charge == currentTrailState)
                {
                    isShootingDuel = false;
                    if(countUnits == trail.Count)
                    {
                        Destroy(energyPulseA);
                        frame_B.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                        Destroy(energyPulseB);
                        frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                        break;
                    }
                }
                if (TrailState.Duel == currentTrailState
                        && countUnits >= trail.Count / 2)
                {
                    Destroy(energyPulseA);
                    frame_B.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    Destroy(energyPulseB);
                    frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    break;
                }

                shootingCountdown = energyPulseA.GetComponent<EnergyPulse>().energyPulseWaitTime;
            }
            countUnits = 0;
        }
    }
    private IEnumerator ShootEnergyPulseA()
    {
        int countUnits = 0;
        shootingCountdown = 5f;
        yield return new WaitForSeconds(.5f); //segura alguns frames para impedir que ShootEnergyPulse seja executado masi de 1 vez
        
        if(TrailState.Charge == currentTrailState)
        {            
            //instanciar a energia!
            GameObject energyPulse = Instantiate(energyPrefab, trail[0].transform.position + new Vector3(0, 0, -1f), Quaternion.identity);

            //passar atributos. talvez eu só deixe de usar
            energyPulse.GetComponent<EnergyPulse>().energyPulseWaitTime = frame_A.thisPixeltron.GetComponent<Pixeltron>()._energyPulseWaitTime;
            energyPulse.GetComponent<EnergyPulse>().energyPulseTimeToMove = frame_A.thisPixeltron.GetComponent<Pixeltron>()._energyPulseTimeToMove;
            
            foreach (TrailUnit unit in trail)
            {
                countUnits += 1;
                //mover a energia até a posição da unidade
                LeanTween.move(energyPulse, unit.transform.position + new Vector3(0, 0, -1f), energyPulse.GetComponent<EnergyPulse>().energyPulseTimeToMove);
                yield return new WaitUntil(() => (unit.transform.position.x - .05f <= energyPulse.transform.position.x && energyPulse.transform.position.x <= unit.transform.position.x + .05f)
                                              && (unit.transform.position.y - .05f <= energyPulse.transform.position.y && energyPulse.transform.position.y <= unit.transform.position.y + .05f));
                
                //if (energyPulse.transform.position.x >= trail[trail.Count - 1].transform.position.x && energyPulse.transform.position.y >= trail[trail.Count - 1].transform.position.y)
                if ((trail[trail.Count - 1].transform.position.x - .25f <= energyPulse.transform.position.x && energyPulse.transform.position.x <= trail[trail.Count - 1].transform.position.x + .25f)
                 && (trail[trail.Count - 1].transform.position.y - .25f <= energyPulse.transform.position.y && energyPulse.transform.position.y <= trail[trail.Count - 1].transform.position.y + .25f))
                {
                    //yield return new WaitForSeconds(energyPulse.GetComponent<EnergyPulse>().energyPulseTimeToMove);
                    //finalizou agora destroi e envia x energia pro alvo
                    Destroy(energyPulse);
                    frame_B.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);

                } else if (TrailState.Neutral == currentTrailState)
                {
                    Destroy(energyPulse);
                    isFrameAShooting = false;
                    break;
                } else if (TrailState.Duel == currentTrailState)
                {
                    isShootingDuel = true;
                    if (unit.faction.currentFaction == frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction)
                    {
                        Destroy(energyPulse);
                        frame_B.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                        break;
                    }
                }
                shootingCountdown = energyPulse.GetComponent<EnergyPulse>().energyPulseWaitTime;
            }
            countUnits = 0;
        }
    }
    private IEnumerator ShootEnergyPulseB()
    {
        int countUnits = 0;
        shootingCountdown = 5f;
        yield return new WaitForSeconds(.5f); //segura alguns frames para impedir que ShootEnergyPulse seja executado masi de 1 vez

        if (TrailState.Charge == currentTrailState)
        {
            //instanciar a energia!
            GameObject energyPulse = Instantiate(energyPrefab, reverseTrail[0].transform.position + new Vector3(0, 0, -1f), Quaternion.identity);

            //passar atributos. talvez eu só deixe de usar
            energyPulse.GetComponent<EnergyPulse>().energyPulseWaitTime = frame_B.thisPixeltron.GetComponent<Pixeltron>()._energyPulseWaitTime;
            energyPulse.GetComponent<EnergyPulse>().energyPulseTimeToMove = frame_B.thisPixeltron.GetComponent<Pixeltron>()._energyPulseTimeToMove;

            foreach (TrailUnit unit in reverseTrail)
            {
                countUnits += 1;
                //mover a energia até a posição da unidade
                LeanTween.move(energyPulse, unit.transform.position + new Vector3(0, 0, -1f), energyPulse.GetComponent<EnergyPulse>().energyPulseTimeToMove);
                yield return new WaitUntil(() => (unit.transform.position.x - .05f <= energyPulse.transform.position.x && energyPulse.transform.position.x <= unit.transform.position.x + .05f)
                                                && (unit.transform.position.y - .05f <= energyPulse.transform.position.y && energyPulse.transform.position.y <= unit.transform.position.y + .05f));

                //if (energyPulse.transform.position.x >= trail[trail.Count - 1].transform.position.x && energyPulse.transform.position.y >= trail[trail.Count - 1].transform.position.y)
                if ((reverseTrail[reverseTrail.Count - 1].transform.position.x - .25f <= energyPulse.transform.position.x && energyPulse.transform.position.x <= reverseTrail[reverseTrail.Count - 1].transform.position.x + .25f)
                    && (reverseTrail[reverseTrail.Count - 1].transform.position.y - .25f <= energyPulse.transform.position.y && energyPulse.transform.position.y <= reverseTrail[reverseTrail.Count - 1].transform.position.y + .25f))
                {
                    //yield return new WaitForSeconds(energyPulse.GetComponent<EnergyPulse>().energyPulseTimeToMove);
                    //finalizou agora destroi e envia x energia pro alvo
                    Destroy(energyPulse);
                    frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);

                }
                else if (TrailState.Neutral == currentTrailState)
                {
                    Destroy(energyPulse);
                    isFrameBShooting = false;
                    break;
                }
                else if (TrailState.Duel == currentTrailState)
                {
                    isShootingDuel = true;
                    if (unit.faction.currentFaction == frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction)
                    {
                        Destroy(energyPulse);
                        frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                        break;
                    }
                }
                shootingCountdown = energyPulse.GetComponent<EnergyPulse>().energyPulseWaitTime;
            }
            countUnits = 0;
        }
    }
    
        

private IEnumerator ChargeTrailAtoB(Faction.FactionName pixeltronFaction, float countTime = 0.2f)
    {
        currentTrailState = TrailState.Charge;
        Faction.FactionName actingFaction = pixeltronFaction;
        int countUnits = 0;
        foreach (TrailUnit unit in trail)
        {
            countUnits += 1;
            yield return new WaitForSeconds(countTime);
            if (countUnits > trail.Count / 2 && currentTrailState == TrailState.Duel)
            {
                frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += (countUnits - (trail.Count/2)) - 1;
                frame_A._energyPointsUI.text = frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
                break;
            }
            if(frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints >= 2){
                frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints -= 1;
                unit.UpdateToFaction(actingFaction);
                frame_A._energyPointsUI.text = frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
            }
            else
            {
                currentTrailState = TrailState.Neutral;
                foreach (TrailUnit reverseUnit in reverseTrail)
                {
                    //só fazer a opeção aonde estiver com as units alteradas
                    if (reverseTrail.IndexOf(reverseUnit) > reverseTrail.IndexOf(unit))
                    {
                        yield return new WaitForSeconds(countTime);
                        frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += 1;
                        //cor da unidade adjacente
                        reverseUnit.UpdateToFaction(reverseTrail[reverseTrail.IndexOf(reverseUnit)-1].faction.currentFaction);
                        
                        frame_A._energyPointsUI.text = frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
                    }
                }
                frame_A.RemoveTrailFromActiveConnections(this);
                break;
            }
        }
        //pode desparar energia! do A -> B dESTE Trail!
        isFrameAShooting = true; 
        //StartCoroutine(ShootEnergyPulseA());
        //StopCoroutine(ChargeTrailAtoB(pixeltronFaction));
    }
    private IEnumerator ChargeTrailBtoA(Faction.FactionName pixeltronFaction, float countTime = 0.2f)
    {
        currentTrailState = TrailState.Charge;
        Faction.FactionName actingFaction = pixeltronFaction;
        int countUnits = 0;

        foreach (TrailUnit reverseUnit in reverseTrail)
        {
            countUnits += 1;
            yield return new WaitForSeconds(countTime);
            if (countUnits > trail.Count / 2 && currentTrailState == TrailState.Duel)
            {
                frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += (countUnits - (trail.Count / 2)) - 1;
                frame_B._energyPointsUI.text = frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
                break;
            }
            if (frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints >= 2)
            {
                frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints -= 1;
                reverseUnit.UpdateToFaction(actingFaction);
                frame_B._energyPointsUI.text = frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
            }
            else
            {
                currentTrailState = TrailState.Neutral;
                //pega a partir da posição anterior e fazer o caminho contrario e então BREAK ChargeTrail
                //por fim -> break;
                foreach (TrailUnit unit in trail)
                {
                    //só fazer a opeção aonde estiver com as units alteradas
                    if (trail.IndexOf(unit) > trail.IndexOf(reverseUnit))
                    {
                        yield return new WaitForSeconds(countTime);
                        frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += 1;
                        unit.UpdateToFaction(trail[trail.Count - 1].faction.currentFaction);
                        frame_B._energyPointsUI.text = frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
                    }
                }
                frame_B.RemoveTrailFromActiveConnections(this);
                break;
            }
        }
        //evento pode desparar energia! do B -> A dESTE Trail!
        isFrameBShooting = true;
        //StopCoroutine(ChargeTrailBtoA(pixeltronFaction));
    }

    private IEnumerator DuelTrailAtoB(Faction.FactionName pixeltronFaction, float countTime = 0.2f)
    {
        currentTrailState = TrailState.Duel;
        int countUnits = 0;
        Faction.FactionName actingFaction = pixeltronFaction;
        foreach (TrailUnit unit in trail)
        {
            countUnits += 1;
            yield return new WaitForSeconds(countTime);
            if (frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints >= 2)
            {
                frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints -= 1;
                unit.UpdateToFaction(actingFaction);
                frame_A._energyPointsUI.text = frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
            }
            else
            {
                //  ele começa com 3 pontos pra usar e ser um por segmento tomado;
                //    xxX0.0000 parou pq pts<2; parou no X index: 2; perdeu 2!
                //    xxX0.0000 -> 0000.0Xxx reverse trail; agora X index é 5
                //    xxX0.0000 -> O000.0Xxx percorre reverse
                //    xxX0.0000 -> oooo.oOxx  o reverse unit chegou na posição do index de X (5);
                //    00X0.0000 -> oooo.oooO  Todas a posições após o X serão recuadas;

                currentTrailState = TrailState.Charge;
                foreach (TrailUnit reverseUnit in reverseTrail)
                {
                    //só fazer a opeção aonde estiver com as units alteradas
                    if (reverseTrail.IndexOf(reverseUnit) > reverseTrail.IndexOf(unit))
                    {
                        yield return new WaitForSeconds(countTime);
                        frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += 1;
                        //cor da unidade adjacente
                        reverseUnit.UpdateToFaction(reverseTrail[reverseTrail.IndexOf(reverseUnit) - 1].faction.currentFaction);

                        frame_A._energyPointsUI.text = frame_A.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
                    }
                }
                frame_A.RemoveTrailFromActiveConnections(this);
                break;
            }
            if (countUnits >= trail.Count / 2)
                break;
        }
        //evento pode desparar energia! do A -> B dESTE Trail!
        isFrameAShooting = true; 
        //StartCoroutine(ShootEnergyPulseA());
        //StopCoroutine(DuelTrailAtoB(pixeltronFaction));
    }
    private IEnumerator DuelTrailBtoA(Faction.FactionName pixeltronFaction, float countTime = 0.2f)
    {
        currentTrailState = TrailState.Duel;
        int countUnits = 0;
        Faction.FactionName actingFaction = pixeltronFaction;
        foreach (TrailUnit reverseUnit in reverseTrail)
        {
            countUnits += 1;
            yield return new WaitForSeconds(countTime);

            if (frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints >= 2)
            {
                frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints -= 1;
                reverseUnit.UpdateToFaction(actingFaction);
                frame_B._energyPointsUI.text = frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
            }
            else
            {
                currentTrailState = TrailState.Charge;
                //pega a partir da posição anterior e fazer o caminho contrario e então BREAK ChargeTrail
                //por fim -> break;
                foreach (TrailUnit unit in trail)
                {
                    //só fazer a opeção aonde estiver com as units alteradas
                    if (trail.IndexOf(unit) > trail.IndexOf(reverseUnit))
                    {
                        yield return new WaitForSeconds(countTime);
                        frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += 1;
                        unit.UpdateToFaction(trail[trail.Count - 1].faction.currentFaction);
                        frame_B._energyPointsUI.text = frame_B.thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
                    }
                }
                frame_B.RemoveTrailFromActiveConnections(this);
                break;
            }

            if (countUnits >= trail.Count / 2)
                break;
        }
        //evento pode desparar energia! do B -> A dESTE Trail!
        isFrameBShooting = true;
        //StopCoroutine(DuelTrailBtoA(pixeltronFaction));
    }
    private IEnumerator CancelDuel(string unitId)
    {
        float countTime = 0.2f;
        int unitIndex = int.Parse(unitId.Substring(unitId.IndexOf("-") + 1)) - 1;

        foreach (TrailUnit unit in trail)
        {
            if (unit.unitId == unitId)
            {
                currentTrailState = TrailState.Canceling;
                //Decobrir se o player está no frame A ou B primeiro
                if (frame_A.transform.Find("pixeltron").gameObject.GetComponent<Pixeltron>().faction.currentFaction == Faction.FactionName.Player)
                {
                    StopCoroutine(ShootEnergyPulseA());
                    isShootingDuel = false;
                    isFrameAShooting = false;
                    for (int i = (trail.Count / 2)-1; i >= 0; i--)
                    {
                        yield return new WaitForSeconds(countTime);
                        trail[i].UpdateToFaction(frame_B.transform.Find("pixeltron").gameObject.GetComponent<Pixeltron>().faction.currentFaction);
                        frame_A.ChangeEnergyValue(1, frame_A.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    //remover a flag de conexão ativa e remover da lista de _activeConnectios a atual trilha 
                    frame_A.RemoveTrailFromActiveConnections(this);
                }
                else if (frame_B.transform.Find("pixeltron").gameObject.GetComponent<Pixeltron>().faction.currentFaction == Faction.FactionName.Player)
                {
                    StopCoroutine(ShootEnergyPulseB());
                    isShootingDuel = false;
                    isFrameBShooting = false;
                    for (int i = trail.Count / 2; i < trail.Count; i++)
                    {
                        yield return new WaitForSeconds(countTime);
                        trail[i].UpdateToFaction(frame_A.transform.Find("pixeltron").gameObject.GetComponent<Pixeltron>().faction.currentFaction);
                        frame_B.ChangeEnergyValue(1, frame_B.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    //remover a flag de conexão ativa e remover da lista de _activeConnectios a atual trilha 
                    frame_B.RemoveTrailFromActiveConnections(this);
                }
                currentTrailState = TrailState.Charge;
                TrailNormalizer();
                break;
            }
        }

    }
    private IEnumerator CancelCharge(string unitId)
    {
        float countTime = 0.2f;
        TrailUnit unitTarget;
        foreach (TrailUnit unit in trail)
        {
            if (unit.unitId == unitId)
            {
                //descobri quem é essa unidade
                unitTarget = unit;
                break;
            }
        }

        //converter String ID na int index da unit na lista do trail
        int unitIndex = int.Parse(unitId.Substring(unitId.IndexOf("-")+1))-1;
        int unitChargingIndex = unitIndex;
        int unitRetreatingIndex = unitIndex;

        //vai saber qual frame atacou atravez a lista de trilhos Ativados de cada frame. O que tiver com trilho ativado é quem estár cancelando o ataque.
        //qual do frame_A ou frame_B tem esse trail na lista _activeConnections; Esse será o que está cancelando.
        
        foreach (TrailScript frameTrail in frame_A._activeConnections)
        {
            StopCoroutine(ShootEnergyPulseA());
            isFrameAShooting = false;
            //frame_A é quem estava atacando e agora cancelando o charge
            if (frameTrail.trailId == trailId)
            {
                currentTrailState = TrailState.Canceling;
                //a partir daí se ele for o Frame A do corte para trás(IDs decrescente) volta pro frame A e o restante avança para o frame B.  //[A] (-id)<--/-->(+id) B//

                //simultaneamente mover parte das unidades para uma direção e mover outra parte para direção oposta.
                ////////////Breve explicação da logica aplicada
                ////////////    EXEMPLO:
                ////////////index: 5(6ºpos)
                ////////////Tamanho do Trilho: 8 int

                ////////////8 count
                ////////////----Cresc
                ////////////5(6º) < 8 true
                ////////////6(7º) < 8 true
                ////////////7(8º) < 8 true
                ////////////8(9º) < 8 false -x-
                ////////////---- - Decresc
                ////////////4(5º) >= 0 true
                ////////////3(4º) >= 0 true
                ////////////2(3º) >= 0 true
                ////////////1(2º) >= 0 true
                ////////////0(1º) >= 0 true
                ////////////-1(0º) >= 0 false -x-

                ////////////simultaneo

                ////////////OOOOOXOO->index: 5        Primeiro muda o unit selecionado
                ////////////OOOOXoXO->index: 4 e 6    depois faz ao mesmo tempo pra +1 e -1 no unitIndex
                ////////////OOOXoooX->index: 3 e 7    como no exemplo o unitIndex so vai até onde unitIndex < 8 for verdadeiro
                ////////////OOXooooo->index: 2        o outro lado continua verdadeiro então ele continua a operação
                ////////////OXoooooo->index: 1
                ////////////Xooooooo->index: 0        aqui é a ultima vez que unitIndex >= 0 é verdadeiro
                ////////////oooooooo->resultado

                ////////////3pts avançando
                ////////////5pts recuando

                //Aplicando a lógica
                while (unitChargingIndex < trail.Count || unitRetreatingIndex >= 0)
                {
                    if (unitChargingIndex == unitRetreatingIndex)
                    {
                        //se for a primeira vez só executar 1 vez
                        yield return new WaitForSeconds(countTime);
                        trail[unitIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim somar e subtrair 1 em cada
                        unitChargingIndex += 1;
                        unitRetreatingIndex -= 1;
                        //mandando 1 ponto para o frame defendendo / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_B.ChangeEnergyValue(1,frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    else if (unitChargingIndex < trail.Count && unitRetreatingIndex >= 0)
                    {
                        //executar uma atualização para cada index
                        yield return new WaitForSeconds(countTime);
                        trail[unitChargingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        trail[unitRetreatingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim somar e subtrair 1 em cada index
                        unitChargingIndex += 1;
                        unitRetreatingIndex -= 1;
                        //2 metodos mandando 1 ponto para ambos os frames / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_A.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                        frame_B.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    else if (unitRetreatingIndex >= 0)
                    {
                        //executar uma atualização
                        yield return new WaitForSeconds(countTime);
                        trail[unitRetreatingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim subtrair 1 do index
                        unitRetreatingIndex -= 1;
                        //1 EVENTo mandando 1 ponto para o frame atacando / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_A.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    else if (unitChargingIndex < trail.Count)
                    {
                        //executar uma atualização
                        yield return new WaitForSeconds(countTime);
                        trail[unitChargingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim subtrair 1 do index
                        unitChargingIndex += 1;
                        //1 EVENTo mandando 1 ponto para o frame defendendo / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_B.ChangeEnergyValue(1, frame_A.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                }
                //remover a flag de conexão ativa e remover da lista de _activeConnectios a atual trilha 
                frame_A.RemoveTrailFromActiveConnections(this);
                currentTrailState = TrailState.Neutral;


            }
        }
        foreach (TrailScript frameTrail in frame_B._activeConnections)
        {
            isFrameBShooting = false;
            //frame_B é quem estava atacando e agora cancelando o charge
            if (frameTrail.trailId == trailId)
            {
                currentTrailState = TrailState.Canceling;
                //se for o Frame B a após o corte as unidades recuam para o frame B (IDs Crescentes) e o restante avança para o frame A.  //A (-id)<--/-->(+id) [B]//
                while (unitRetreatingIndex < trail.Count || unitChargingIndex >= 0)
                {
                    if (unitChargingIndex == unitRetreatingIndex)
                    {
                        //se for a primeira vez só executar 1 vez
                        yield return new WaitForSeconds(countTime);
                        trail[unitIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim somar e subtrair 1 em cada
                        unitChargingIndex -= 1;
                        unitRetreatingIndex += 1;
                        //mandando 1 ponto para o frame defendendo / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    else if (unitRetreatingIndex < trail.Count && unitChargingIndex >= 0)
                    {
                        //executar uma atualização para cada index
                        yield return new WaitForSeconds(countTime);
                        trail[unitChargingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        trail[unitRetreatingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim somar e subtrair 1 em cada index
                        unitChargingIndex -= 1;
                        unitRetreatingIndex += 1;
                        //2 metodos mandando 1 ponto para ambos os frames / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                        frame_B.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    else if (unitRetreatingIndex < trail.Count )
                    {
                        //executar uma atualização
                        yield return new WaitForSeconds(countTime);
                        trail[unitRetreatingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim subtrair 1 do index
                        unitRetreatingIndex += 1;
                        //mandando 1 ponto para o frame atacando / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_B.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                    else if (unitChargingIndex >= 0)
                    {
                        //executar uma atualização
                        yield return new WaitForSeconds(countTime);
                        trail[unitChargingIndex].UpdateToFaction(Faction.FactionName.Neutral);
                        //no fim subtrair 1 do index
                        unitChargingIndex -= 1;
                        //mandando 1 ponto para o frame defendendo / se for facção igual ele somar / se for facção diferente ele sibtrai
                        frame_A.ChangeEnergyValue(1, frame_B.thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                    }
                }
                //remover a flag de conexão ativa e remover da lista de _activeConnectios a atual trilha 
                frame_B.RemoveTrailFromActiveConnections(this);
                currentTrailState = TrailState.Neutral;
            }
        }
    }
    private void CancelConnectionHandler(string unitId)
    {
        if (currentTrailState == TrailState.Charge && trail[0].faction.currentFaction == Faction.FactionName.Player && trail[trail.Count - 1].faction.currentFaction == Faction.FactionName.Player)
        {
            //usar isso pra cortar e transferir pontos da posição específica.
            foreach (TrailUnit unit in trail)
            {
                if (unit.unitId == unitId)
                {
                    //currentTrailState = TrailState.Neutral;
                    StartCoroutine(CancelCharge(unitId));    
                }
            }

        } else if (currentTrailState == TrailState.Duel)
        {
            //currentTrailState = TrailState.Charge;
            StartCoroutine(CancelDuel(unitId));
        }

    }

    private FrameScript currentFrame;
    private void ConnectionHandler(int actingId, int targetId)
    {
        ////sem uso
        foreach (FrameScript frameGO in FindObjectsOfType<FrameScript>())
        {
            if (frameGO.id == actingId)
            {
                currentFrame = frameGO;
            }
        }
        /////
        
        Faction.FactionName pixeltronFaction = frame_A.transform.Find("pixeltron").gameObject.GetComponent<Pixeltron>().faction.currentFaction;
        if (frame_A.id == actingId && frame_B.id == targetId 
            && currentFrame._activeConnections.Count < currentFrame._maxConnections)
        {
            if (currentTrailState == TrailState.Neutral)
            {
                //mexer na UI e controle de conexões
                FrameConnectionsUI(actingId);
                //realizar a animação de conexão
                StartCoroutine(ChargeTrailAtoB(pixeltronFaction));
            }
            else if (currentTrailState == TrailState.Charge && trail[0].faction.GetFaction() != pixeltronFaction && trail[trail.Count - 1].faction.GetFaction() != pixeltronFaction)
            {
                //mexer na UI e controle de conexões
                FrameConnectionsUI(actingId);
                //realizar a animação de conexão
                StartCoroutine(DuelTrailAtoB(pixeltronFaction));
            }
        }
        else if (frame_A.id == targetId && frame_B.id == actingId
            && currentFrame._activeConnections.Count < currentFrame._maxConnections)
        {
            pixeltronFaction = frame_B.transform.Find("pixeltron").gameObject.GetComponent<Pixeltron>().faction.currentFaction;
            if (currentTrailState == TrailState.Neutral)
            {
                //mexer na UI e controle de conexões
                FrameConnectionsUI(actingId);
                //realizar a animação de conexão
                StartCoroutine(ChargeTrailBtoA(pixeltronFaction));
            }
            else if (currentTrailState == TrailState.Charge && trail[0].faction.GetFaction() != pixeltronFaction && trail[trail.Count - 1].faction.GetFaction() != pixeltronFaction)
            {
                //mexer na UI e controle de conexões
                FrameConnectionsUI(actingId);
                //realizar a animação de conexão
                StartCoroutine(DuelTrailBtoA(pixeltronFaction));
            }
        }
    
    }
    private void TrailNormalizer()
    {
        if (currentTrailState == TrailState.Charge && trail[0].faction.currentFaction == trail[trail.Count - 1].faction.currentFaction)
        {
            foreach (TrailUnit unit in trail)
            {
                unit.UpdateToFaction(trail[0].faction.currentFaction);
            }
        }
    }
    private void FrameConnectionsUI(int actingId)
    {
        foreach (FrameScript frameGO in FindObjectsOfType<FrameScript>())
        {
            if (frameGO.id == actingId)
            {
                frameGO.AddTrailToActiveConnections(this.GetComponent<TrailScript>());
            }
        }
    }

    #region Building_LEVEL
    private void FulfillTrail()
    {
        TrailUnit[] allTrailUnit = transform.GetComponentsInChildren<TrailUnit>();
        foreach (TrailUnit tu in allTrailUnit)
        {
            trail.Add(tu);
        }
    }
    //O objetivo é ligar os frames aos trilho por proximidade designando o inicio(A) e o fim(B)
    //relacionado posição na lista e os frames corretos
    //e depois usar isso para definir a direção dos movimentos de trailsUnits nos cancelamentos! 

    private void SetAdjacentFrames()
    {
        TrailUnit firstUnit = trail[0];
        TrailUnit lastUnit = trail[trail.Count - 1];

        FrameScript frameA = null;
        FrameScript frameB = null;

        if (firstUnit.transform.name == "unit_I")
        {
            frameA = UnitICollision(firstUnit.transform);
        }
        else if (firstUnit.transform.name == "unit_L")
        {
            frameA = UnitLCollision(firstUnit.transform);
        }
        if (lastUnit.transform.name == "unit_I")
        {
            frameB = UnitICollision(lastUnit.transform);
        }
        else if (lastUnit.transform.name == "unit_L")
        {
            frameB = UnitLCollision(lastUnit.transform);
        }

        frame_A = frameA;
        frame_B = frameB;
    }
    private List<int> DetectAdjacentFramesIds()
    {
        TrailUnit firstUnit = trail[0];
        TrailUnit lastUnit = trail[trail.Count - 1];

        FrameScript frameA = null;
        FrameScript frameB = null;

        if (firstUnit.transform.name == "unit_I")
        {
            frameA = UnitICollision(firstUnit.transform);
        }
        else if (firstUnit.transform.name == "unit_L")
        {
            frameA = UnitLCollision(firstUnit.transform);
        }
        if (lastUnit.transform.name == "unit_I")
        {
            frameB = UnitICollision(lastUnit.transform);
        }
        else if (lastUnit.transform.name == "unit_L")
        {
            frameB = UnitLCollision(lastUnit.transform);
        }

        return new List<int> { frameA.id, frameB.id };
    }
    private FrameScript UnitICollision(Transform unitITransform) {
        float unitDimension = .5f;
        Vector3 newPos = unitITransform.position;
        Vector3 direction;
        if (unitITransform.eulerAngles.z == 180) //se é Horizontal
        {
            //Para a esquerda
            newPos.x = unitITransform.position.x - new Vector3(unitDimension, 0, 0).x;
            direction = newPos - unitITransform.position;
            ray = new Ray(unitITransform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<FrameScript>() != null)
                {
                    gizmoPoint = hit.point;
                    return hit.collider.GetComponent<FrameScript>();
                }
                else
                {
                    //trocar a direção para direita
                    newPos.x = unitITransform.position.x + new Vector3(unitDimension, 0, 0).x;
                    direction = newPos - unitITransform.position;
                    ray = new Ray(unitITransform.position, direction);
                    if (Physics.Raycast(ray, out RaycastHit hit2))
                    {
                        if (hit2.collider.gameObject.GetComponent<FrameScript>() != null)
                        {
                            gizmoPoint = hit2.point;
                            return hit2.collider.GetComponent<FrameScript>();
                        }
                    }
                }
            }
        }
        if (unitITransform.eulerAngles.z == 270) //se é Vertical
        {
            //Para baixo
            newPos.y = unitITransform.position.y - new Vector3(0, unitDimension, 0).y;
            direction = newPos - unitITransform.position;
            ray = new Ray(unitITransform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<FrameScript>() != null)
                {
                    gizmoPoint = hit.point;
                    return hit.collider.GetComponent<FrameScript>();
                }
                else
                {
                    //trocar a direção para cima
                    newPos.y = unitITransform.position.y + new Vector3(0, unitDimension, 0).y;
                    direction = newPos - unitITransform.position;
                    ray = new Ray(unitITransform.position, direction);
                    if (Physics.Raycast(ray, out RaycastHit hit2))
                    {
                        if (hit2.collider.gameObject.GetComponent<FrameScript>() != null)
                        {
                            gizmoPoint = hit2.point;
                            return hit2.collider.GetComponent<FrameScript>();
                        }
                    }
                }
            }
        }
        return null;
    }
    private FrameScript UnitLCollision(Transform unitLTransform)
    {
        //referencia de posição e rays
        ////
        // rot.z == 0 //euler.z == 180 : para cima(+y) e para esquerda(-x)
        // rot.z == 90 //euler.z == 270 : para cima(+y) e para direita(+x)
        // rot.z == -180 //euler.z == 0 : para baixo(-y) e para direita(+x)
        // rot.z == -90 //euler.z == 90 : para baixo(-y) e para esquerda(-x)
        ////

        float unitDimension = .5f;
        Vector3 newPos = unitLTransform.position;
        Vector3 direction;
        if (unitLTransform.eulerAngles.z == 180) //se é para cima(+y) e para esquerda(-x)
        {
            //Para a cima
            newPos.y = unitLTransform.position.y + new Vector3(0,unitDimension,0).y;
            direction = newPos - unitLTransform.position;
            ray = new Ray(unitLTransform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<FrameScript>() != null)
                {
                    gizmoPoint = hit.point;
                    return hit.collider.GetComponent<FrameScript>();
                }
                else
                {
                    //trocar a direção para esquerda
                    newPos.x = unitLTransform.position.x - new Vector3(unitDimension, 0, 0).x;
                    direction = newPos - unitLTransform.position;
                    ray = new Ray(unitLTransform.position, direction);
                    if (Physics.Raycast(ray, out RaycastHit hit2))
                    {
                        if (hit2.collider.gameObject.GetComponent<FrameScript>() != null)
                        {
                            gizmoPoint = hit2.point;
                            return hit2.collider.GetComponent<FrameScript>();
                        }
                    }
                }
            }
        }
        else if (unitLTransform.eulerAngles.z == 270) //se é para cima(+y) e para direita(+x)
        {
            //Para a cima
            newPos.y = unitLTransform.position.y + new Vector3(0, unitDimension, 0).y;
            direction = newPos - unitLTransform.position;
            ray = new Ray(unitLTransform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<FrameScript>() != null)
                {
                    gizmoPoint = hit.point;
                    return hit.collider.GetComponent<FrameScript>();
                }
                else
                {
                    //trocar a direção para esquerda
                    newPos.x = unitLTransform.position.x + new Vector3(unitDimension, 0, 0).x;
                    direction = newPos - unitLTransform.position;
                    ray = new Ray(unitLTransform.position, direction);
                    if (Physics.Raycast(ray, out RaycastHit hit2))
                    {
                        if (hit2.collider.gameObject.GetComponent<FrameScript>() != null)
                        {
                            gizmoPoint = hit2.point;
                            return hit2.collider.GetComponent<FrameScript>();
                        }
                    }
                }
            }
        }
        else if (unitLTransform.eulerAngles.z == 0) //se é para baixo(-y) e para direita(+x)
        {
            //Para a cima
            newPos.y = unitLTransform.position.y - new Vector3(0, unitDimension, 0).y;
            direction = newPos - unitLTransform.position;
            ray = new Ray(unitLTransform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<FrameScript>() != null)
                {
                    gizmoPoint = hit.point;
                    return hit.collider.GetComponent<FrameScript>();
                }
                else
                {
                    //trocar a direção para esquerda
                    newPos.x = unitLTransform.position.x + new Vector3(unitDimension, 0, 0).x;
                    direction = newPos - unitLTransform.position;
                    ray = new Ray(unitLTransform.position, direction);
                    if (Physics.Raycast(ray, out RaycastHit hit2))
                    {
                        if (hit2.collider.gameObject.GetComponent<FrameScript>() != null)
                        {
                            gizmoPoint = hit2.point;
                            return hit2.collider.GetComponent<FrameScript>();
                        }
                    }
                }
            }
        }
        else if (unitLTransform.eulerAngles.z == 90) //se é para baixo(-y) e para esquerda(-x)
        {
            //Para a cima
            newPos.y = unitLTransform.position.y - new Vector3(0, unitDimension, 0).y;
            direction = newPos - unitLTransform.position;
            ray = new Ray(unitLTransform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<FrameScript>() != null)
                {
                    gizmoPoint = hit.point;
                    return hit.collider.GetComponent<FrameScript>();
                }
                else
                {
                    //trocar a direção para esquerda
                    newPos.x = unitLTransform.position.x - new Vector3(unitDimension, 0, 0).x;
                    direction = newPos - unitLTransform.position;
                    ray = new Ray(unitLTransform.position, direction);
                    if (Physics.Raycast(ray, out RaycastHit hit2))
                    {
                        if (hit2.collider.gameObject.GetComponent<FrameScript>() != null)
                        {
                            gizmoPoint = hit2.point;
                            return hit2.collider.GetComponent<FrameScript>();
                        }
                    }
                }
            }
        }
        return null;
    }
    #endregion

    #region DEBUG_FIELD
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(gizmoPoint, 0.2f);
        Gizmos.color = Color.black;
        Gizmos.DrawRay(ray);
    }
    public void ToggleState()
    {
        Debug.Log("Togolei");
        if (currentTrailState == TrailState.Neutral)
        {
            currentTrailState = TrailState.Charge;
        }
        else
        {
            currentTrailState = TrailState.Neutral;
        }

    }
    void ShowState()
    {
        Debug.Log("Current State of trail"+trailId+": "+currentTrailState);
    }
    #endregion
}
