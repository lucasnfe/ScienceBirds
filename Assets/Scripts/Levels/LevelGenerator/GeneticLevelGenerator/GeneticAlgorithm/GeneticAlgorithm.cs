﻿using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Levels.LevelGenerator;

/** \class GeneticAlgorithm
 *  \brief  Contains the variables, methods and main loop of the genetic algorithm
 *
 *  Contains info about the population and generation sizes, mutation and crossorver rates, total fitness, elitism, 
 *  The actual and next generation, as well as the delegation of methods of crossover, mutation, fitness function and genome initialization.
 *  Also has methods to rank genomes, start evolution of population, creating next generation and tournament.
 */
public class GeneticAlgorithm<T> {

	// Genetic Algorithm delegates ("Pointes to functions")
    /**
     *  Delegate of genome initialization function
     *  @param[out] values   initialized genomes
     */
	public delegate void GAInitGenome(out T values);

    /**
     *  Delegate of genome initialization function using pre-population
     *  @param[out] values  initialized genomes
     *  @param[in]  level   loaded level
     */
    public delegate void GAInitGenomePrePop(out T values, ShiftABLevel level);
    /**
     *  Delegate of the crossover function
     *  @param[in]  genome1 First genome to cross
     *  @param[in]  genome2 Second genome to cross
     *  @param[out] child1  First child generated by crossover
     *  @param[out] child2  Second child generated by crossover
     */
    public delegate void GACrossover(ref Genome<T> genome1, ref Genome<T> genome2, out Genome<T> child1, out Genome<T> child2);
    /**
     *  Delegate of the mutation function
     *  @param[out] genome1 Genome to be mutated and returned.
     */
    public delegate void GAMutation (ref Genome<T> genome1);
    /**
     *  Delegate of the fitness function
     *  @param[out] values  value of the calculated fitness
     *  @param[in]  genomeIdx   index of genome to calculate the fitness of
     */
	public delegate float GAFitnessFunction(T values, int genomeIdx);

	// Genetic Algorithm attributes
    /**Size of the population*/
	private int _populationSize;
    /**Size of the generations*/
	private int _generationSize;
    /**Mutation rate*/
	private float _mutationRate;
    /**Crossover rate*/
    private float _crossoverRate;
    /**Total fitness of a popultion*/
    private float _totalFitness;
    /**Boolean to choose if will have elitism or not*/
	private bool _elitism;

	// Genetic Algorithm data structures
    /**Genomes of the actual generation*/
	private ArrayList _thisGeneration;
    /**Genomes of the next generation*/
	private ArrayList _nextGeneration;
    /**"Pointer" to the fitness method*/
    static private GAFitnessFunction getFitness;
    /**"Pointer" to the initialize genome method*/
    static private GAInitGenome getInitGenome;
    /**"Pointer" to the initialize genome method*/
    static private GAInitGenomePrePop getInitGenomePrePop;
    /**"Pointer" to the crossover method*/
    static private GACrossover  getCrossover;
    /**"Pointer" to the mutation method*/
    static private GAMutation   getMutation;
    
    /**The Intance Manager object*/
    private DataMiningManager _dataMiningManager = new DataMiningManager();
    /**Classifier to be used to classify the levels*/
    private weka.classifiers.Classifier cl;
    /**Constructor setting by default the values for mutation and crossover rates, and population and generation sizes*/
    public GeneticAlgorithm() {

		_mutationRate = 0.05f;
		_crossoverRate = 0.80f;
		_populationSize = 100;
		_generationSize = 2000;
	}

    /**
     *  Constructor receiving the values for mutation and crossover rates, and population and generation sizes, as well as if 
     *  Will exist elitism.
     *  @param[in] crossoverRate    The crossover rate
     *  @param[in] mutationRate     The mutation rate
     *  @param[in] populationSize   The population size
     *  @param[in] generationSize   The total number of generations
     *  @param[in] elitism          True if GA will have elitism, false otherwise
     */
    public GeneticAlgorithm(float crossoverRate, float mutationRate, int populationSize, int generationSize, bool elitism = false) {

		_mutationRate = mutationRate;
		_crossoverRate = crossoverRate;
		_populationSize = populationSize;
		_generationSize = generationSize;
		_elitism = elitism;
	}

    /**Accessor for the fitness function*/
	public GAFitnessFunction FitnessFunction {

		get  {
			return getFitness;
		}
		set {
			getFitness = value;
		}
	}
    /**Accessor for the crossover function*/
    public GACrossover Crossover {
		
		get  {
			return getCrossover;
		}
		set {
			getCrossover = value;
		}
	}
    /**Accessor for the mutation function*/
    public GAMutation Mutation {
		
		get  {
			return getMutation;
		}
		set {
			getMutation = value;
		}
	}
    /**Accessor for the genome initialization function*/
    public GAInitGenome InitGenome {
		
		get  {
			return getInitGenome;
		}
		set {
			getInitGenome = value;
		}
	}

    /**Accessor for the genome initialization function*/
    public GAInitGenomePrePop InitGenomePrePop
    {

        get
        {
            return getInitGenomePrePop;
        }
        set
        {
            getInitGenomePrePop = value;
        }
    }
    //  Properties
    /**Accessor for the population size variable*/
    public int PopulationSize {

		get {
			return _populationSize;
		}
		set {
			_populationSize = value;
		}
	}
    /**Accessor for the generation size variable*/
    public int Generations {

		get {
			return _generationSize;
		}
		set {
			_generationSize = value;
		}
	}
    /**Accessor for the crossover rate variable*/
    public float CrossoverRate {

		get {
			return _crossoverRate;
		}
		set {
			_crossoverRate = value;
		}
	}
    /**Accessor for the mutation rate variable*/
    public float MutationRate {

		get {
			return _mutationRate;
		}
		set {
			_mutationRate = value;
		}
	}

    /// Keep previous generation's fittest individual in place of worst in current
    /**Accessor for the elitism variable*/
    public bool Elitism {

		get {
			return _elitism;
		}
		set {
			_elitism = value;
		}
	}
	/**
     *  Gets the best individual of the population, that is, the one in position 0
     *  @param[out] values  genes of the best individual
     *  @param[out] fitness fitness of the best individual
     */
	public void GetBest(out T values, out float fitness) {

		// _thisGeneration.Sort(new GenomeComparer<T>());
		GetNthGenome(0, out values, out fitness);
	}
    /**
     *  Gets the worst individual of the population, that is, the one in the last position
     *  @param[out] values  genes of the worst individual
     *  @param[out] fitness fitness of the worst individual
     */
    public void GetWorst(out T values, out float fitness) {

		// _thisGeneration.Sort(new GenomeComparer<T>());
		GetNthGenome(_populationSize - 1, out values, out fitness);
	}
    /**
     *  Gets the Nth individual of the population, the one in position n
     *  @param[in]  n       The index of the desired individual
     *  @param[out] values  Genes of the nth individual
     *  @param[out] fitness Fitness of the nth individual
     */
    public void GetNthGenome(int n, out T values, out float fitness) {

		if (n < 0 || n > _populationSize - 1)
			throw new ArgumentOutOfRangeException("n too large, or too small");

		Genome<T> g = ((Genome<T>)_thisGeneration[n]);

		values = g.Genes;
		fitness = (float)g.Fitness;
	}

	// Method which starts the GA executing.
    /**
     *  Starts the evolution of the GA, checking if the delegate functions have been supplied,
     *  Creates the current and next generations arrays. Sets the mutation rate of the genome
     *  And initializes the population with randomly generated levels. 
     */
	public void StartEvolution() {

		if (getFitness == null)
			throw new ArgumentNullException("Need to supply fitness function");

		if (getInitGenome == null)
			throw new ArgumentNullException("Need to supply initialization function");

		if (getCrossover == null)
			throw new ArgumentNullException("Need to supply crossover function");

		if (getMutation == null)
			throw new ArgumentNullException("Need to supply mutation function");

		// Create current and next generations
		_thisGeneration = new ArrayList(_generationSize);
		_nextGeneration = new ArrayList(_generationSize);

		Genome<T>.MutationRate = _mutationRate;

		InitializePopulation();
	}
    //Starts a genome with a fixed pre population
    /**
     *  Starts the evolution of the GA, checking if the delegate functions have been supplied,
     *  Creates the current and next generations arrays. Sets the mutation rate of the genome
     *  And initializes the population with levels loaded from a data set. 
     */
    public void StartEvolutionPrePop()
    {

        if (getFitness == null)
            throw new ArgumentNullException("Need to supply fitness function");

        if (getInitGenome == null)
            throw new ArgumentNullException("Need to supply initialization function");

        if (getCrossover == null)
            throw new ArgumentNullException("Need to supply crossover function");

        if (getMutation == null)
            throw new ArgumentNullException("Need to supply mutation function");

        // Create current and next generations
        _thisGeneration = new ArrayList(_generationSize);
        _nextGeneration = new ArrayList(_generationSize);
        Genome<T>.MutationRate = _mutationRate;
        //Load all the level genomes from a folder
        ShiftABLevel[] levels = LevelLoader.LoadAllGenomes();
        //Initialize the population with the loaded levels
        InitializePopulationWithPrePop(levels);
    }
    /**
     *  Do the tournament selection, selecting randomly two individuals and returning the one with better fitness.
     *  @param[in]  size    size of the tournament
     *  @return     Genome<T>   Winning genome.
     */
    private Genome<T> TournamentSelection(int size = 2) {

		Genome<T> []tournamentPopulation = new Genome<T>[size];

		for(int i = 0; i < size; i++)
			tournamentPopulation[i] = (Genome<T>)_thisGeneration[UnityEngine.Random.Range(0, _thisGeneration.Count)];

		Array.Sort(tournamentPopulation, new GenomeComparer<T>());
	
		return tournamentPopulation[0];
	}
	/**
     *  Creates the next generation by clearing the array for next generationg, and creating a new population
     *  by choosing, 2-by-2, individuals from the actual generation by tournament, doing the crossover to generate
     *  two children, mutating the children and adding them to the new generation. If there is elitism, substitutes
     *  a random individual from the new generation for the best one from the old generation.
     *  Then, clears the actual generation array and adds the new generation individuals in the actual generation array.
     */
	public void CreateNextGeneration()
	{
		_nextGeneration.Clear();
		for (int i = 0; i < _populationSize; i += 2) {
			
			Genome<T> parent1, parent2, child1, child2;

			parent1 = ((Genome<T>) TournamentSelection());
            parent2 = ((Genome<T>)TournamentSelection());
			
			Crossover(ref parent1, ref parent2, out child1, out child2);
			
			Mutation(ref child1);
			Mutation(ref child2);
			
			_nextGeneration.Add(child1);
			_nextGeneration.Add(child2);
		}
		
		if (_elitism)
			_nextGeneration[UnityEngine.Random.Range(0, _nextGeneration.Count)] = (Genome<T>)_thisGeneration[0];
		
		_thisGeneration.Clear();

        for (int i = 0; i < _populationSize; i++)
			_thisGeneration.Add(_nextGeneration[i]);
	}

	/** 
     *  Rank population and sort in order of fitness. And calculates its total fitness
     */
	public void RankPopulation() {

		_totalFitness = 0f;

		for (int i = 0; i < _populationSize; i++) {
			
			Genome<T> g = ((Genome<T>) _thisGeneration[i]);
			g.Fitness = FitnessFunction(g.Genes, i);
			_totalFitness += g.Fitness;
		}
		
		_thisGeneration.Sort(new GenomeComparer<T>());
	}
	
	/** 
     *  Create the initial genomes by calling the supplied InitGenome() function and adds them to the actual generation.
     */
	private void InitializePopulation() {
        cl = (weka.classifiers.trees.RandomForest)weka.core.SerializationHelper.read(_dataMiningManager.modelClassifierPath);

        for (int i = 0; i < _populationSize ; i++) {
			
			Genome<T> g = new Genome<T>();

            T genes = g.Genes;
            /*
             * Creates only levels that would be classified as finishable
             */
            if (GeneticLG.setCreatePrePopulation)
            {
                AngryBirdsGen ABgenes;
                do
                {
                    InitGenome(out genes);
                    ABgenes = (AngryBirdsGen)Convert.ChangeType(genes, typeof(AngryBirdsGen));
                } while ((_dataMiningManager.EvaluateUsingClassifier(ABgenes.level, cl) == 0));
            }
            else
            {
                InitGenome(out genes);
            }
            g.Genes = genes;
			_thisGeneration.Add(g);
		}
	}

    /** 
     *  Create the initial genomes by calling the supplied InitGenome() function and adds them to the actual generation.
     */
    private void InitializePopulationWithPrePop(ShiftABLevel[] levels)
    {
        cl = (weka.classifiers.trees.RandomForest)weka.core.SerializationHelper.read(_dataMiningManager.modelClassifierPath);

        for (int i = 0; i < _populationSize; i++)
        {

            Genome<T> g = new Genome<T>();

            T genes = g.Genes;
            /*
             * Creates only levels that would be classified as finishable
             */
            if (GeneticLG.setCreatePrePopulation)
            {
                AngryBirdsGen ABgenes;
                do
                {
                    InitGenome(out genes);
                    ABgenes = (AngryBirdsGen)Convert.ChangeType(genes, typeof(AngryBirdsGen));
                } while ((_dataMiningManager.EvaluateUsingClassifier(ABgenes.level, cl) == 0));
            }
            else
            {
                InitGenomePrePop(out genes, levels[i]);
            }
            g.Genes = genes;
            _thisGeneration.Add(g);
        }
    }
}