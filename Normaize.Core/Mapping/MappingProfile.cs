using AutoMapper;
using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // DataSet mappings
        CreateMap<DataSet, DataSetDto>();
        CreateMap<CreateDataSetDto, DataSet>();
        
        // Analysis mappings
        CreateMap<Analysis, AnalysisDto>();
        CreateMap<CreateAnalysisDto, Analysis>();
    }
} 