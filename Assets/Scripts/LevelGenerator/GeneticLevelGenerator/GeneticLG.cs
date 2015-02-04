﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public struct AngryBirdsGen 
{
	public int birdsAmount;
	public List<LinkedList<ShiftABGameObject>> gameObjects;
	
	public AngryBirdsGen(int birdsAmount)
	{
		this.birdsAmount = birdsAmount;
		this.gameObjects = new List<LinkedList<ShiftABGameObject>>();
	}
}

public class GeneticLG : RandomLG 
{	
	private int _generationIdx;
	private int _genomeIdx;

	private bool _isRankingGenome;

	// Fitness function variables
	private float _pk, _pi, _lk, _li, _bi, _bk;

	public int _populationSize, _generationSize;
	public float _mutationRate, _crossoverRate;
	public bool _elitism;

	private List<float> _fitnessTable;

	GeneticAlgorithm<AngryBirdsGen> _geneticAlgorithm;

	public override List<ABGameObject> GenerateLevel()
	{
		List<ABGameObject> gameObjects = new List<ABGameObject>();
		
		return gameObjects;
	}
	
	public override void Start()
	{
		base.Start();

		_fitnessTable = new List<float>();

		GameWorld.Instance.ClearWorld();

		// Generate a population of feaseble levels evaluated by an inteligent agent
		_geneticAlgorithm = new GeneticAlgorithm<AngryBirdsGen>(_crossoverRate, _mutationRate, _populationSize, _generationSize, _elitism);

		_geneticAlgorithm.InitGenome = new GeneticAlgorithm<AngryBirdsGen>.GAInitGenome(InitAngryBirdsGenome);
		_geneticAlgorithm.Mutation = new GeneticAlgorithm<AngryBirdsGen>.GAMutation(Mutate);
		_geneticAlgorithm.Crossover = new GeneticAlgorithm<AngryBirdsGen>.GACrossover(Crossover);
		_geneticAlgorithm.FitnessFunction = new GeneticAlgorithm<AngryBirdsGen>.GAFitnessFunction(EvaluateUsingAI);
		
		_geneticAlgorithm.StartEvolution();

		_generationIdx = 0;
		_genomeIdx = 0;

		_isRankingGenome = false;

		// Set time scale to acelerate evolution
		Time.timeScale = 2f;
	}

	void Update()
	{
		if(!_isRankingGenome)
		{
			double fitness = 0f;
			AngryBirdsGen genome = new AngryBirdsGen();
			_geneticAlgorithm.GetNthGenome(_genomeIdx, out genome, out fitness);

			StartEvaluatingGenome(genome);
		}
	
		if(_isRankingGenome && GameWorld.Instance.IsLevelStable() && 
		   (GameWorld.Instance.GetBirdsAvailableAmount() == 0 || 
		    GameWorld.Instance.GetPigsAvailableAmount() == 0))
		{
			_bk = GameWorld.Instance.GetBirdsAvailableAmount();
			_pk = GameWorld.Instance.GetPigsAvailableAmount();
			_lk = GameWorld.Instance.GetBlocksAvailableAmount();

			_fitnessTable.Add(Fitness((int)_pk, (int)_pi, (int)_li, (int)_bi, (int)_bk));

			GameWorld.Instance.ClearWorld();

			_genomeIdx++;
			_isRankingGenome = false;

			if(_genomeIdx == _geneticAlgorithm.PopulationSize)
			{
				Debug.Log("====== GENERATION " + _generationIdx +  " ======");
				
				_geneticAlgorithm.RankPopulation();
				_fitnessTable.Clear();

				AngryBirdsGen genome = GetCurrentBest();
				
				if(_generationIdx < _geneticAlgorithm.Generations)
				{
					_geneticAlgorithm.CreateNextGeneration();
				}
				else
				{
					Debug.Log("====== END EVOLUTION ======");
					Time.timeScale = 1f;
					
					// Clear the level and decode the best genome of the last generation
					GameWorld.Instance.ClearWorld();			
					DecodeLevel(ConvertShiftGBtoABGB(genome.gameObjects), genome.birdsAmount);				
					
					// Disable AI and allow player to test the level
					GameWorld.Instance._birdAgent.gameObject.SetActive(false);
					
					Destroy(this.gameObject);
				}
				
				_genomeIdx = 0;
				_generationIdx++;
			}
		}
	}

	private float Fitness(int pk, int pi, int li, int bi, int bk)
	{
		float fitness;
		
		if(pk != 0 || pi == 0)
			
			fitness = -1f;
		else
			fitness = (li + pi);
		
		return fitness;
	}

	private void StartEvaluatingGenome(AngryBirdsGen genome)
	{
		DecodeLevel(ConvertShiftGBtoABGB(genome.gameObjects), genome.birdsAmount);

		_bi = GameWorld.Instance.GetBirdsAvailableAmount();
		_pi = GameWorld.Instance.GetPigsAvailableAmount();
		_li = GameWorld.Instance.GetBlocksAvailableAmount();

		_isRankingGenome = true;
	}

	public double EvaluateUsingAI(AngryBirdsGen genome, int genomeIdx)
	{
		return _fitnessTable[genomeIdx];
	}

	public void Crossover(ref Genome<AngryBirdsGen> genome1, ref Genome<AngryBirdsGen> genome2, 
	                      out Genome<AngryBirdsGen> child1,  out Genome<AngryBirdsGen> child2) {

		int maxGenomeSize = Mathf.Max (genome1.Genes.gameObjects.Count, 
		                               genome2.Genes.gameObjects.Count);

		child1 = new Genome<AngryBirdsGen>();
		child2 = new Genome<AngryBirdsGen>();

		AngryBirdsGen genes1 = new AngryBirdsGen(0);
		AngryBirdsGen genes2 = new AngryBirdsGen(0);
	
		for(int i = 0; i < maxGenomeSize; i++)
		{
			if(genome1.Genes.gameObjects.Count == genome2.Genes.gameObjects.Count)
			{
				if(UnityEngine.Random.value < 0.5f)

					genes1.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
				else
					genes1.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));

				if(UnityEngine.Random.value < 0.5f)

					genes2.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
				else
					genes2.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));
			}
			else if(genome1.Genes.gameObjects.Count < genome2.Genes.gameObjects.Count)
			{
				if(i < genome1.Genes.gameObjects.Count)
				{
					if(UnityEngine.Random.value < 0.5f)

						genes1.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
					else
						genes1.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));

					if(UnityEngine.Random.value < 0.5f)

						genes2.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
					else
						genes2.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));
				}
				else
				{
					if(UnityEngine.Random.value < 0.5f)

						genes1.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));
					else
						genes1.gameObjects.Add(new LinkedList<ShiftABGameObject>());
					
					if(UnityEngine.Random.value < 0.5f)

						genes2.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));
					else
						genes2.gameObjects.Add(new LinkedList<ShiftABGameObject>());
				}
			}
			else
			{
				if(i < genome2.Genes.gameObjects.Count)
				{
					if(UnityEngine.Random.value < 0.5f)

						genes1.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
					else
						genes1.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));

					if(UnityEngine.Random.value < 0.5f)

						genes2.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
					else
						genes2.gameObjects.Add(CopyStack(genome2.Genes.gameObjects[i]));
				}
				else
				{
					if(UnityEngine.Random.value < 0.5f)

						genes1.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
					else
						genes1.gameObjects.Add(new LinkedList<ShiftABGameObject>());
					
					if(UnityEngine.Random.value < 0.5f)

						genes2.gameObjects.Add(CopyStack(genome1.Genes.gameObjects[i]));
					else
						genes2.gameObjects.Add(new LinkedList<ShiftABGameObject>());
				}
			}
		}

		// Integer crossover for birds
		genes1.birdsAmount = (int)(0.5f * genome1.Genes.birdsAmount + 0.5f * genome2.Genes.birdsAmount);
		genes2.birdsAmount = (int)(1.5f * genome1.Genes.birdsAmount - 0.5f * genome2.Genes.birdsAmount);

		child1.Genes = genes1;
		child2.Genes = genes2;
	}
	
	public void Mutate(ref Genome<AngryBirdsGen> genome) {

		List<LinkedList<ShiftABGameObject>> gameObjects = genome.Genes.gameObjects;

		for(int i = 0; i < genome.Genes.gameObjects.Count; i++)
		{
			if(UnityEngine.Random.value <= _geneticAlgorithm.MutationRate)
			{
				gameObjects[i] = new LinkedList<ShiftABGameObject>();
				GenerateNextStack(i, ref gameObjects);
				InsertPigs(i, ref gameObjects);
				genome.Genes.gameObjects[i] = gameObjects[i];
			}
		}
	}

	public void InitAngryBirdsGenome(out AngryBirdsGen genome) {

		genome.birdsAmount = DefineBirdsAmount();
		genome.gameObjects = GenerateRandomLevel();
	}

	private AngryBirdsGen GetCurrentBest() {

		double fitness = 0f;
		AngryBirdsGen genome = new AngryBirdsGen();
		_geneticAlgorithm.GetBest(out genome, out fitness);

		return genome;
	}
}