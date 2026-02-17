@{
    ModuleVersion     = '0.1.0'
    GUID              = 'b3f7a2d1-8c4e-4f6b-9d2a-1e5c3b7a9f04'
    Author            = 'BusinessCentral.AL.Mutations Contributors'
    Description       = 'Mutation testing tool for Business Central AL code'
    PowerShellVersion = '5.1'
    RootModule        = 'BCMutations.psm1'
    FunctionsToExport = @(
        'Invoke-BCMutationTest',
        'New-BCMutationConfig',
        'Get-BCMutationOperators'
    )
    PrivateData       = @{
        PSData = @{
            Tags       = @('BusinessCentral', 'AL', 'MutationTesting', 'Testing')
            ProjectUri = 'https://github.com/StefanMaron/BusinessCentral.AL.Mutations'
        }
    }
}
