@{
    ModuleVersion     = '0.1.0'
    GUID              = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
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
