﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineBuilding : BuildingBase
{
    public Transform digPos;
    public float richness = 1;//资源丰度

    public override void InitBuildingFunction()
    {
        base.InitBuildingFunction();
    }

    public override void OnConfirmBuild(Vector2Int[] vector2Ints)
    {
        buildFlag = true;
        gameObject.tag = "Building";
        takenGrids = vector2Ints;
        if (hasAnima)
        {
            Invoke("PlayAnim", 0.2f);
        }
        parkingGridIn = GetInParkingGrid();
        parkingGridOut = GetOutParkingGrid();
        transform.GetComponent<BoxCollider>().enabled = false;
        transform.GetComponent<BoxCollider>().enabled = true;
        //MapManager.Instance.BuildFoundation(vector2Ints, 7, (int)direction);
        Vector3 targetPos = MapManager.GetTerrainPosition(parkingGridIn);
        float targetHeight = targetPos.y;
        TerrainGenerator.Instance.FlatGround(takenGrids, targetHeight);
        InitBuildingFunction();
        richness = SetRichness(takenGrids);
    }

    protected override void Output()
    {
        if (formula == null || formula.OutputItemID == null)
        {
            Debug.LogError("矿井配方为空");
        }
        productTime--;
        if (productTime <= 0)
        {
            productTime = formula.ProductTime;
            float rate = runtimeBuildData.Rate;
            CarMission carMission = MakeCarMission(rate);
            TrafficManager.Instance.UseCar(carMission, () => carMission.EndBuilding.OnRecieveCar(carMission));
            runtimeBuildData.Rate = 0;
        }
    }

    public float SetRichness(Vector2Int[] takenGrids)
    {
        float sum = 0;
        float total = takenGrids.Length;
        for (int i = 0; i < takenGrids.Length; i++)
        {
            sum += MapManager.GetMineRichness(takenGrids[i]);
        }
        return Mathf.Clamp01(sum / total*3);
    }
    protected override void Input()
    {
        base.Input();
        DigGround();
    }

    protected override CarMission MakeCarMission(float rate)
    {
        //Debug.Log(rate);
        CarMission mission = new CarMission();
        mission.StartBuilding = this;
        mission.EndBuilding = MapManager.GetNearestMarket(parkingGridIn).GetComponent<BuildingBase>();
        mission.missionType = CarMissionType.transportResources;
        mission.isAnd = true;
        mission.transportResources = new List<CostResource>();
        mission.transportationType = TransportationType.sandTruck;
        for (int i = 0; i < formula.OutputItemID.Count; i++)
        {
            //Debug.Log(formula.OutputItemID[i]);
            mission.transportResources.Add(new CostResource(formula.OutputItemID[i], rate * formula.ProductNum[i] * richness * runtimeBuildData.Times));

        }
        return mission;
    }

    private void DigGround()
    {
        float height = MapManager.GetTerrainPosition(digPos.position).y;
        Vector2Int[] grids = BuildManager.Instance.GetAllGrids(5, 5, digPos.position, false);
        TerrainGenerator.Instance.FlatGround(grids,height-0.1f);
    }
}
