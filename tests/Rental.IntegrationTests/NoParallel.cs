using Xunit;

// define uma coleção com paralelismo desativado
[CollectionDefinition("NoParallel", DisableParallelization = true)]
public class NoParallelCollectionDefinition { }
