// Generated File. Do not edit.
// File created by `Compiler.Name` compiler version `Compiler.Version`-`Compiler.Timestamp`
// on `System.Date` at `System.Time` UTC

`FORALL Guid IN TraceGuids`
`Guid.Text` `Guid.Comment`
`FORALL Msg in Guid.Messages`
#typev `Msg.Name` `Msg.MsgNo` "`Msg.Text`"
{
`FORALL Arg IN Msg.Arguments`
  `Arg.Name`, `Arg.MofType` // `Arg.No`
`ENDFOR Arg`
}
`ENDFOR Msg`
`ENDFOR Guid`
