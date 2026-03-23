using HoRang2Sea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HoRang2Sea.Services
{
    public class JsonDataParser
    {
        //private string buffer = string.Empty;
        //private const char seperator = '|';
        public JsonDataParser()
        {
        }
        public bool ParseData(string rawdata, Action<List<DataModel>> goodData)
        {
            //buffer += rawdata;
            //string packet = string.Empty;
            string packet = rawdata;
            List<DataModel> data = new List<DataModel>();

            //if (!buffer.Contains(seperator))
            //{
            //    goodData(new List<DataModel>());
            //    return false;
            //}
            //else
            //{
            //    var b = buffer.Split(seperator);
            //    packet = b[0];
            //    var bb = new List<string>();
            //    for (int i = 1; i < b.Length; i++)
            //    {
            //        bb.Add(b[i]);
            //    }
            //    buffer = string.Join(seperator, bb);
            //}

            try
            {
                RecordData[]? recordData = JsonSerializer.Deserialize<RecordData[]>(packet);
                if (recordData != null)
                {
                    foreach (var r in recordData)
                    {
                        data.Add(new DataModel(r.Name, r.Type, r.Value)
                        {
                            Time = r.Time,
                            ID = r.ID,
                            Module = r.Module,
                            Unit = r.Unit,
                            Description = r.Description,
                        });
                    }
                }
            }
            //if (recordDataSet != null)
            //{
            //    foreach (var recordData in recordDataSet)
            //    {
            //        if (recordData != null)
            //        {
            //            data.Add(new DataModel(recordData.Name, recordData.Type, recordData.Value)
            //            {
            //                Time = recordData.Time,
            //                ID = recordData.ID,
            //                Module = recordData.Module,
            //                Unit = recordData.Unit,
            //                Description = recordData.Description,
            //            });
            //        }
            //switch (recordData.Type)
            //{
            //    case "System.Double":
            //    case "System.Single":
            //    case "System.Int16":
            //    case "System.Int32":
            //    case "System.Int64":
            //    case "System.String":
            //    case "System.Boolean":
            //    default:
            //        {
            //            data.Add(new DataModel(recordData.Type)
            //            {
            //                Time = recordData.Time,
            //                ID = recordData.ID,
            //                Module = recordData.Module,
            //                Name = recordData.Name,
            //                Unit = recordData.Unit,
            //                Description = recordData.Description,
            //                ValueString = recordData.Value,
            //            });
            //            break;
            //        }
            //    case "System.Double[]":
            //    case "System.Single[]":
            //        {
            //            data.Add(new DataModel()
            //            {
            //                Time = recordData.Time,
            //                ID = recordData.ID,
            //                Module = recordData.Module,
            //                Name = recordData.Name,
            //                Unit = recordData.Unit,
            //                Description = recordData.Description,
            //                ValueString = recordData.Value,
            //            });
            //            break;
            //            //var names = recordData.Name.Split(',');
            //            //var units = recordData.Unit.Split(',');
            //            //if (names.Length == recordData.Length)
            //            //{
            //            //    for (int i = 0; i < names.Length; i++)
            //            //    {
            //            //        data.Add(new DataModel()
            //            //        {
            //            //            Time = recordData.Time,
            //            //            ID = recordData.ID,
            //            //            Module = recordData.Module,
            //            //            Name = names[i],
            //            //            Unit = units[i],
            //            //            Description = recordData.Description,
            //            //            ValueString = recordData.Value,
            //            //        });
            //            //    }
            //            //    break;
            //            //else
            //            //{
            //            //    return false;
            //            //}
            //        }
            //}
            //        }
            //    }
            //}
            catch
            {
                //buffer = String.Empty;
                goodData(new List<DataModel>());
                return false;
            }

            //buffer = string.Empty;
            goodData(data);
            return true;
        }
    }
}
