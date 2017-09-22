﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MyNEAT
{
    class GNeuron
    {
        //public List<int> inConnections;
        //public List<int> outConnections;
        public bool isOutput;
        public bool isInput;
        public bool isBias;
        public bool isHidden;

        public readonly int Id;

        public GNeuron(int id)
        {
            //inConnections = new List<int>();
            //outConnections = new List<int>();
            isBias = false;
            isHidden = false;
            isOutput = false;
            isInput = false;
            Id = id;
        }

        public override string ToString()
        {
            string str = "This id: ";
            str += Id;
            str += ", " + "Is input: " + isInput + ", " + "Is bias: " + isBias + ", " + "Is Output: " + isOutput;

            return str;
        }
    }

    class GConnection
    {
        public readonly int id;
        public readonly double weight;
        public readonly int fromNeuron;
        public readonly int toNeuron;
        public GConnection(GNeuron fromneuron, GNeuron toneuron, double wei, int idForThis)
        {
            fromNeuron = fromneuron.Id;
            toNeuron = toneuron.Id;

            id = idForThis;

            weight = wei;
        }
        public GConnection(int fromneuron, int toneuron, double wei, int idForThis)
        {
            fromNeuron = fromneuron;
            toNeuron = toneuron;

            id = idForThis;

            weight = wei;
        }
        public override string ToString()
        {
            string str = "This id: " + id + ", ";
            str += "This weight: " + Math.Round(weight, 2) + ", |||";
            str += "From: " + fromNeuron + ", ";
            str += "To: " + toNeuron + ", ";
            return str;
        }


    }

    public class Genome
    {
        public float fitness;

        internal List<GNeuron> neurons;
        internal List<GConnection> connections;

        public static double connWeightRange = 5d;
        public static double weightChangeRange = 0.5;

        public static double probabilityOfMutation = 0.9;
        public static double probabilityOfResetWeight = 0.2;
        public static double probabilityOfChangeWeight = 0.6;
        public static double probabilityAddNeuron = 0.1;
        public static double probabilityRemoveNeuron = 0.01;
        public static double probabilityAddConnection = 0.3;
        public static double probabilityRemoveConnection = 0.1;

        public static int geneIndex;

        #region Constructors
        public Genome(int inputs, int outputs)
        {
            geneIndex = 0;
            neurons = new List<GNeuron>();
            connections = new List<GConnection>();

            for (int i = 0; i < inputs; i++)//only inputs
            {
                GNeuron inpNeuron = new GNeuron(geneIndex);
                inpNeuron.isInput = true;
                geneIndex++;

                neurons.Add(inpNeuron);
            }

            GNeuron biasNeuron = new GNeuron(geneIndex);
            biasNeuron.isBias = true;
            geneIndex++;
            neurons.Add(biasNeuron);

            for (int i = 0; i < outputs; i++)//only output neurons
            {
                GNeuron outNeuron = new GNeuron(geneIndex);
                outNeuron.isOutput = true;
                geneIndex++;

                neurons.Add(outNeuron);
            }

            Random randGenerator = new Random();
            foreach (GNeuron neuron in neurons)
            {
                if (neuron.isOutput)
                {

                    foreach (GNeuron neuron1 in neurons)
                    {
                        if (neuron1.isInput || neuron1.isBias)
                        {
                            GConnection conn = new GConnection(neuron1, neuron, randGenerator.NextDouble() * (connWeightRange - (-connWeightRange)) + (-connWeightRange), geneIndex);
                            geneIndex++;

                            connections.Add(conn);
                        }
                    }
                }
            }


        }


        public Genome()
        {
        }
        #endregion

        #region Mutators
        public static Genome MutationChangeWeight(Random generator, Genome genome)
        {
            if (genome.connections.Count != 0)
            {
                int num = generator.Next(genome.connections.Count);
                GConnection conn = genome.connections[num];
                if (generator.NextDouble() < probabilityOfResetWeight)
                {
                    genome.connections[num] = new GConnection(conn.fromNeuron, conn.toNeuron, generator.NextDouble() * (connWeightRange - (-connWeightRange)) + (-connWeightRange), conn.id);

                }
                else
                {
                    genome.connections[num] = new GConnection(conn.fromNeuron, conn.toNeuron, genome.connections[num].weight + generator.NextDouble() * (weightChangeRange - (-weightChangeRange)) + (-weightChangeRange), conn.id);
                }
            }
            return genome;
        }

        public static Genome MutationAddNeuron(Random generator, Genome genome)
        {
            if (genome.connections.Count != 0)
            {
                int ind = generator.Next(genome.connections.Count);
                GConnection conn = genome.connections[ind];
                genome.connections.RemoveAt(ind);

                GNeuron newNeuron = new GNeuron(Genome.geneIndex);
                genome.neurons.Add(newNeuron);
                Genome.geneIndex++;

                GConnection newConnIn = new GConnection(conn.fromNeuron, newNeuron.Id, 1, Genome.geneIndex);
                genome.connections.Add(newConnIn);
                Genome.geneIndex++;

                GConnection newConnOut = new GConnection(newNeuron.Id, conn.toNeuron, conn.weight, Genome.geneIndex);
                genome.connections.Add(newConnOut);
                Genome.geneIndex++;
            }

            return genome;
        }

        public static Genome MutationRemoveNeuron(Random generator, Genome genome)
        {
            List<GNeuron> availableNeurons = new List<GNeuron>();
            for (int i = 0; i < genome.neurons.Count; i++)
            {
                int[] inOut = Genome.FindAmountOfInAndOutConnectionsForNeuronWithId(genome.connections, genome.neurons[i].Id);
                if (inOut[0] == 0 && inOut[1] == 0 && genome.neurons[i].isInput != true && genome.neurons[i].isOutput != true && genome.neurons[i].isBias != true)
                {
                    availableNeurons.Add(genome.neurons[i]);
                }
            }
            if (availableNeurons.Count != 0)
            {
                GNeuron toDelete = availableNeurons[generator.Next(availableNeurons.Count)];
                genome.neurons.Remove(toDelete);
            }

            return genome;
        }

        public static Genome MutationAddConnection(Random generator, Genome genome)
        {
            GNeuron neuron1 = genome.neurons[generator.Next(genome.neurons.Count)];
            GNeuron neuron2 = genome.neurons[generator.Next(genome.neurons.Count)];

            var n1InOut = GetListOfInAndOutConnections(genome.connections, neuron1.Id);
            var n2InOut = GetListOfInAndOutConnections(genome.connections, neuron2.Id);
            if (n1InOut[1].Intersect(n2InOut[0]).Count() == 0 && n1InOut[0].Intersect(n2InOut[1]).Count() == 0)
            {
                genome.connections.Add(new GConnection(neuron1.Id, neuron2.Id,
                    generator.NextDouble() * (connWeightRange - (-connWeightRange)) + (-connWeightRange),
                    Genome.geneIndex));
                Genome.geneIndex++;
            }
            return genome;
        }

        public static Genome MutationRemoveConnection(Random generator, Genome genome)
        {
            if (genome.connections.Count != 0)
            {
                GConnection connToRemove = genome.connections[generator.Next(genome.connections.Count)];
                genome.connections.Remove(connToRemove);
            }

            return genome;
        }
        #endregion

        #region Reproduction
        /// <summary>
        /// Asexual reproduction
        /// </summary>
        /// <returns></returns>
        public Genome CreateOffSpring(Random generator)
        {
            Genome offspring = new Genome();
            offspring.neurons = new List<GNeuron>(neurons);
            offspring.connections = new List<GConnection>(connections);

            if (generator.NextDouble() < probabilityOfMutation)
            {
                offspring = Mutate(generator, offspring);
            }
            return offspring;
        }

        /// <summary>
        /// Sexual reproduction
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="otherParent"></param>
        /// <returns></returns>
        public Genome CreateOffSpring(Random generator, Genome otherParent)
        {
            Genome offspring = Crossover(generator, this, otherParent);

            if (generator.NextDouble() < probabilityOfMutation)
            {
                offspring = Mutate(generator, offspring);
            }
            return offspring;
        }
        #endregion

        #region Static methods
        public static Genome Crossover(Random generator, Genome parent1, Genome parent2)
        {
            List<GNeuron> neurons = new List<GNeuron>();

            #region Build neurons
            for (int i = 0; i < Math.Min(parent1.neurons.Count, parent2.neurons.Count); i++)
            {
                if (parent1.neurons[i].Id == parent2.neurons[i].Id)
                {
                    neurons.Add(parent1.neurons[i]);
                }
                else
                {
                    GNeuron[] arr = new GNeuron[] { parent1.neurons[i], parent2.neurons[i] };
                    GNeuron toAdd = arr[generator.Next(arr.Length)];
                    neurons.Add(toAdd);

                }
            }
            if (neurons.Count != Math.Max(parent1.neurons.Count, parent2.neurons.Count))
            {
                for (int i = Math.Min(parent1.neurons.Count, parent2.neurons.Count); i < Math.Max(parent1.neurons.Count, parent2.neurons.Count); i++)
                {
                    if (Math.Max(parent1.neurons.Count, parent2.neurons.Count) == parent1.neurons.Count)
                    {
                        neurons.Add(parent1.neurons[i]);
                    }
                    else
                    {
                        neurons.Add(parent2.neurons[i]);
                    }
                }
            }
            #endregion

            List<GConnection> connections = new List<GConnection>(Math.Max(parent1.connections.Count, parent2.connections.Count));

            #region Build connections
            List<List<GConnection>> sortedParents = new List<List<GConnection>>();
            sortedParents.Add(parent1.connections);
            sortedParents.Add(parent2.connections);
            sortedParents.Sort((x, y) => x.Count.CompareTo(y.Count));//now i know what has low count and what high

            for (int i = 0; i < sortedParents[1].Count; i++)
            {
                if (i < sortedParents[0].Count)
                {
                    if (sortedParents[0][i].id == sortedParents[1][i].id)
                    {
                        if (IsGWithIdExistsInList(neurons, sortedParents[0][i].fromNeuron) && IsGWithIdExistsInList(neurons, sortedParents[0][i].toNeuron))
                        {
                            connections.Add((sortedParents[0][i]));
                        }
                    }
                    else
                    {
                        GConnection[] arr = new GConnection[] { sortedParents[0][i], sortedParents[1][i] };
                        int num = generator.Next(arr.Length);

                        if (IsGWithIdExistsInList(neurons, arr[num].fromNeuron) && IsGWithIdExistsInList(neurons, arr[num].toNeuron))
                        {
                            connections.Add(arr[num]);
                        }
                        else if (IsGWithIdExistsInList(neurons, arr[Math.Abs(num - 1)].fromNeuron) && IsGWithIdExistsInList(neurons, arr[Math.Abs(num - 1)].toNeuron))
                        {
                            connections.Add(arr[Math.Abs(num - 1)]);
                        }
                    }
                }
                else if (i < sortedParents[1].Count)
                {
                    if (IsGWithIdExistsInList(neurons, sortedParents[1][i].fromNeuron) && IsGWithIdExistsInList(neurons, sortedParents[1][i].toNeuron))
                    {
                        connections.Add(sortedParents[1][i]);
                    }
                }
            }

            /*
            for (int i = 0; i < Math.Min(parent1.connections.Count, parent2.connections.Count); i++)
            {
                if (parent1.connections[i].id == parent2.connections[i].id)
                {
                    if (IsGWithIdExistsInList(neurons, parent1.connections[i].fromNeuron) && IsGWithIdExistsInList(neurons, parent1.connections[i].toNeuron))
                    {
                        connections.Add(parent1.connections[i]);
                    }
                }
                else
                {
                    GConnection[] arr = new GConnection[] { parent1.connections[i], parent2.connections[i] };
                    int num = generator.Next(arr.Length);
                    if (IsGWithIdExistsInList(neurons, arr[num].fromNeuron) && IsGWithIdExistsInList(neurons, arr[num].toNeuron))
                    {
                        connections.Add(arr[num]);
                    }
                    else if (IsGWithIdExistsInList(neurons, arr[Math.Abs(num - 1)].fromNeuron) && IsGWithIdExistsInList(neurons, arr[Math.Abs(num - 1)].toNeuron))
                    {
                        connections.Add(arr[Math.Abs(num - 1)]);
                    }
                    else
                    {

                    }
                }
            }
            if (connections.Count != Math.Max(parent1.connections.Count, parent2.connections.Count))
            {
                //TODO: if there are no connections then i = -1
                for (int i = Math.Min(parent1.connections.Count, parent2.connections.Count) - 1; i < Math.Max(parent1.connections.Count, parent2.connections.Count); i++)
                {

                    if (Math.Max(parent1.connections.Count, parent2.connections.Count) == parent1.connections.Count)
                    {
                        if (IsGWithIdExistsInList(neurons, parent1.connections[i].fromNeuron) && IsGWithIdExistsInList(neurons, parent1.connections[i].toNeuron))
                        {
                            connections.Add(parent1.connections[i]);
                        }
                    }
                    else
                    {
                        if (IsGWithIdExistsInList(neurons, parent2.connections[i].fromNeuron) && IsGWithIdExistsInList(neurons, parent2.connections[i].toNeuron))
                        {
                            connections.Add(parent2.connections[i]);
                        }
                    }
                }
            }*/

            #endregion

            Genome child = new Genome();
            child.neurons = new List<GNeuron>(neurons);
            child.connections = new List<GConnection>(connections);
            return child;
        }

        public static Genome Mutate(Random generator, Genome toMutate)
        {

            if (generator.NextDouble() < probabilityOfChangeWeight)
            {
                toMutate = Genome.MutationChangeWeight(generator, toMutate);
            }
            if (generator.NextDouble() < probabilityAddNeuron)
            {
                toMutate = Genome.MutationAddNeuron(generator, toMutate);
            }
            if (generator.NextDouble() < probabilityRemoveNeuron)
            {
                toMutate = Genome.MutationRemoveNeuron(generator, toMutate);
            }
            if (generator.NextDouble() < probabilityAddConnection)
            {
                toMutate = Genome.MutationAddConnection(generator, toMutate);
            }
            if (generator.NextDouble() < probabilityRemoveConnection)
            {
                toMutate = Genome.MutationRemoveConnection(generator, toMutate);
            }

            return toMutate;
        }

        internal static bool IsGWithIdExistsInList(List<GNeuron> neurons, int id)
        {
            foreach (GNeuron neuron in neurons)
            {
                if (neuron.Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsGWithIdExistsInList(List<GConnection> conns, int id)
        {
            foreach (GConnection conn in conns)
            {
                if (conn.id == id)
                {
                    return true;
                }
            }
            return false;
        }

        internal static int[] FindAmountOfInAndOutConnectionsForNeuronWithId(List<GConnection> connectionList, int id)
        {
            int sumIn = 0;
            int sumOut = 0;
            foreach (GConnection conn in connectionList)
            {
                if (conn.toNeuron == id)
                {
                    sumIn++;
                }
                if (conn.fromNeuron == id)
                {
                    sumOut++;
                }
            }
            return new int[] { sumIn, sumOut };
        }
        internal static List<GConnection>[] GetListOfInAndOutConnections(List<GConnection> connectionList, int id)
        {
            List<GConnection> inConn = new List<GConnection>();
            List<GConnection> outConn = new List<GConnection>();
            foreach (GConnection conn in connectionList)
            {
                if (conn.toNeuron == id)
                {
                    inConn.Add(conn);
                }
                if (conn.fromNeuron == id)
                {
                    outConn.Add(conn);
                }
            }
            return new List<GConnection>[] { inConn, outConn };

        }
        #endregion

        public override string ToString()
        {
            string str = "";
            for (int i = 0; i < neurons.Count; i++)
            {
                str += neurons[i].ToString() + "\n";
            }
            str += "\n";
            for (int i = 0; i < connections.Count; i++)
            {
                str += connections[i].ToString() + "\n";
            }

            return str;
        }

        public int GetComplexity()
        {
            int sum = 0;
            sum += neurons.Count + connections.Count;
            return sum;
        }
    }


    class DNeuron
    {

        #region Public
        public List<DConnection> outConnections;

        public bool isBias;
        public bool isInput;
        public bool isOutput;

        public int amountOfInConnections;

        public int id;

        public List<double> inputsAdded;
        public double output;

        public List<int> depths;
        public int depth;
        #endregion

        public DNeuron(int id)
        {
            depths = new List<int>();

            outConnections = new List<DConnection>();

            this.id = id;
            inputsAdded = new List<double>();
            depth = 0;
            output = 0;
        }
        public void Activate()
        {
            double sum = 0;
            for (int i = 0; i < inputsAdded.Count; i++)
            {
                sum += inputsAdded[i];
            }
            inputsAdded.Clear();

            if (isInput)
            {
                output = sum;//linear
            }
            else if (isBias)
            {
                output = 1;//bias' output is always 1
            }
            else if (isOutput)
            {
                output = Math.Tanh(sum);
            }
            else
            {
                output = sum;//TODO: currently linear
            }

            TransferOutput();
        }

        void TransferOutput()
        {
            foreach (DConnection conn in outConnections)
            {
                conn.toNeuron.inputsAdded.Add(output * conn.weight);
            }
        }

        static public DNeuron FindNeuronWithId(List<DNeuron> neuronslist, int id)
        {
            for (int i = 0; i < neuronslist.Count; i++)
            {
                if (neuronslist[i].id == id)
                {
                    return neuronslist[i];
                }
            }
            throw new Exception();
        }

        #region Depth things
        public void SetDepth()
        {
            if (depths.Count != 0)
            {
                depth = depths.Min();
            }
        }
        #endregion

        public override string ToString()
        {
            string str = "This id: " + id;
            str += ", this n depth: " + depth;
            str += ", " + "Is input: " + isInput + ", " + "Is bias: " + isBias + ", " + "Is Output: " + isOutput + " ||| ";
            str += "Out: ";
            for (int i = 0; i < outConnections.Count; i++)
            {
                str += " Id = " + outConnections[i].toNeuron.id;
                str += " Weight = " + Math.Round(outConnections[i].weight, 2);
                str += " Depth = " + outConnections[i].toNeuron.depth;
            }

            return str;
        }

    }

    class DConnection
    {
        public double weight;
        public DNeuron toNeuron;
    }

    public class Network
    {
        List<DNeuron> dneurons = new List<DNeuron>();
        List<DNeuron> inputs = new List<DNeuron>();
        List<DNeuron> outputs = new List<DNeuron>();
        List<DNeuron> hidden = new List<DNeuron>();

        public Network(Genome genome)
        {
            for (int i = 0; i < genome.neurons.Count; i++)
            {
                dneurons.Add(new DNeuron(genome.neurons[i].Id));
            }

            //iterate through all neurons
            DNeuron currneu;
            foreach (GNeuron neuron in genome.neurons)
            {
                currneu = DNeuron.FindNeuronWithId(dneurons, neuron.Id);

                currneu.amountOfInConnections = Genome.FindAmountOfInAndOutConnectionsForNeuronWithId(genome.connections, neuron.Id)[0];
                if (neuron.isInput || neuron.isBias)
                {
                    //currneu.isInput = true;
                    inputs.Add(currneu);
                    //currneu.amountOfInConnections = neuron.inConnections.Count;
                    if (neuron.isBias)
                    {
                        currneu.isBias = true;
                    }
                    else if (neuron.isInput)
                    {
                        currneu.isInput = true;
                    }
                }
                else if (neuron.isOutput)
                {
                    currneu.isOutput = true;
                    outputs.Add(currneu);
                }
                else
                {
                    hidden.Add(currneu);
                }
            }

            foreach (GConnection conn in genome.connections)
            {
                DConnection connout = new DConnection();
                connout.toNeuron = DNeuron.FindNeuronWithId(dneurons, conn.toNeuron);
                connout.weight = conn.weight;
                DNeuron.FindNeuronWithId(dneurons, conn.fromNeuron).outConnections.Add(connout);

            }

            //another check
            if (inputs.Last<DNeuron>().isBias != true)
            {
                throw new Exception();
            }

            DepthCalculator depthCalculator = new DepthCalculator();
            depthCalculator.SetDepthsToNetwork(inputs);
            foreach (DNeuron n in dneurons)
            {
                n.SetDepth();
            }

            //sort list of neurons
            dneurons.Sort(new Comparer());
        }
        public double[] Predict(double[] state)
        {
            double[] prediction = new double[outputs.Count];

            for (int i = 0; i < inputs.Count - 1; i++)
            {
                inputs[i].inputsAdded.Add(state[i]);
                inputs[i].Activate();
            }
            inputs[inputs.Count - 1].Activate();//bias

            foreach (DNeuron neuron in hidden)
            {
                neuron.Activate();
            }

            foreach (DNeuron neuron in outputs)
            {
                neuron.Activate();
            }

            for (int i = 0; i < outputs.Count; i++)
            {
                prediction[i] = outputs[i].output;
            }
            return prediction;
        }

        public override string ToString()
        {
            string str = "\n";
            //write structure
            for (int i = 0; i < dneurons.Count; i++)
            {
                str += dneurons[i].ToString() + "\n";
            }
            str += "\n";
            return str;
        }
    }

    class DepthCalculator
    {
        public void SetDepthsToNetwork(List<DNeuron> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                int depth = 0;
                List<int> visited = new List<int>();
                SetDepthStartingFromThisNeuron(inputs[i], depth, visited);
            }
        }

        private void SetDepthStartingFromThisNeuron(DNeuron neuron, int depth, List<int> visited)
        {
            neuron.depths.Add(depth);
            visited.Add(neuron.id);

            foreach (DConnection conn in neuron.outConnections)
            {
                if (visited.Contains(conn.toNeuron.id) == false)
                {
                    SetDepthStartingFromThisNeuron(conn.toNeuron, depth + 1, visited);
                }
            }


        }
    }

    class Comparer : IComparer<DNeuron>
    {
        int IComparer<DNeuron>.Compare(DNeuron x, DNeuron y)
        {
            int compareDate = x.depth.CompareTo(y.depth);
            return compareDate;
        }
    }
}