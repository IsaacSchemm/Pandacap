namespace Pandacap.ActivityPub

type IListPage =
    abstract member Current: obj seq
    abstract member Next: string option
