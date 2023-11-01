# multiplay

## 1v1 json for matchmaker service
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
"""
