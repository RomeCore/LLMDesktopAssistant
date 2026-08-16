# Memory

The assistant has a built-in **semantic memory** that works fully locally —
no external vector database required.

## Facts

Facts are short statements about you or your projects. They are stored with
embeddings and retrieved with hybrid search (vector + BM25).

## Logs

Episodic logs record what happened over time. They support:

- Real-time timestamps or an **alternative timeline** (for example, game days)
- Keyword (BM25) search
- Importance filtering

> [!NOTE]
> Memory is optional: automatic recording can be enabled per chat in the
> memory settings.
