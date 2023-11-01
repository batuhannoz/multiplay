# multiplay

## Build configuration
```bash
-ip 0.0.0.0 -port $$port$$ -queryPort $$query_port$$ -logFile $$log_dir$$/matchplaylog.log
```

## 1v1 gamemode json for matchmaker service
```json
{
  "Name": "1v1",
  "MatchDefinition": {
    "Teams": [
      {
        "Name": "Teams",
        "TeamCount": {
          "Min": 1,
          "Max": 2
        },
        "PlayerCount": {
          "Min": 1,
          "Max": 1
        }
      }
    ],
    "MatchRules": []
  },
  "BackfillEnabled": true
}
```
